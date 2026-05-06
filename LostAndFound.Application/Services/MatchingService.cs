using AutoMapper;
using LostAndFound.Application.DTOs.Ai;
using LostAndFound.Application.DTOs.Report;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Application.Services
{
    /// <summary>
    /// Mock matching service. Simulates AI-based matching by comparing
    /// SubCategory, location proximity, and keyword overlap.
    /// </summary>
    public class MatchingService : IMatchingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly IAiService _aiService;
        private readonly ILogger<MatchingService> _logger;

        public MatchingService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            INotificationService notificationService,
            IAiService aiService,
            ILogger<MatchingService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<List<ReportMatchDto>> RunMatchingAsync(int reportId)
        {
            var report = await _unitOfWork.Reports.GetQueryable()
                .Include(r => r.SubCategory)
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                throw new KeyNotFoundException("Report not found");

            var results = new List<ReportMatchDto>();

            // 1) AI-first matching (safe: fallback to legacy matching when unavailable/empty).
            try
            {
                var isPersonReport = IsPersonReport(report.Type);
                var aiResults = await SearchUsingAiAsync(report);
                if (aiResults.Count > 0)
                {
                    var ids = aiResults
                        .Select(r => TryGetAiResultId(r, isPersonReport, out var id) ? id : (int?)null)
                        .Where(id => id.HasValue)
                        .Select(id => id!.Value)
                        .Distinct()
                        .Where(id => id != reportId)
                        .ToList();

                    if (ids.Count > 0)
                    {
                        var candidates = await _unitOfWork.Reports.GetQueryable()
                            .Include(r => r.SubCategory)
                            .Where(r => ids.Contains(r.Id)
                                     && r.CreatedById != report.CreatedById
                                     && r.Status != ReportStatus.Closed)
                            .ToListAsync();

                        var scoreById = aiResults
                            .Where(r => TryGetAiResultId(r, isPersonReport, out _))
                            .GroupBy(r =>
                            {
                                TryGetAiResultId(r, isPersonReport, out var parsedId);
                                return parsedId;
                            })
                            .ToDictionary(
                                g => g.Key,
                                g => Math.Round(g.Max(x => x.Score), 1));

                        foreach (var candidate in candidates)
                        {
                            var aiScore = scoreById.TryGetValue(candidate.Id, out var s) ? s : 0;
                            if (aiScore < 30) continue;
                            await UpsertMatchAndMaybeNotifyAsync(report, candidate, aiScore, results);
                        }

                        await _unitOfWork.SaveChangesAsync();
                        if (results.Count > 0)
                            return results.OrderByDescending(r => r.SimilarityScore).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI matching failed for report {ReportId}. Falling back to legacy matching.", reportId);
            }

            // 2) Legacy fallback matching.
            var fallbackCandidates = await _unitOfWork.Reports.GetQueryable()
                .Include(r => r.SubCategory)
                .Where(r => r.Id != reportId
                         && r.CreatedById != report.CreatedById
                         && r.Status != ReportStatus.Closed
                         && r.SubCategoryId == report.SubCategoryId)
                .Take(100)
                .ToListAsync();

            foreach (var candidate in fallbackCandidates)
            {
                var score = CalculateSimilarity(report, candidate);
                if (score < 30) continue; // Skip very low matches

                await UpsertMatchAndMaybeNotifyAsync(report, candidate, score, results);
            }

            await _unitOfWork.SaveChangesAsync();
            return results.OrderByDescending(r => r.SimilarityScore).ToList();
        }

        private async Task<List<AiResultDto>> SearchUsingAiAsync(Report report)
        {
            var text = string.IsNullOrWhiteSpace(report.Description) ? report.Title : report.Description;
            if (string.IsNullOrWhiteSpace(text))
                return new List<AiResultDto>();

            var (image, imageStream) = TryOpenFirstReportImage(report);
            const int k = 20;
            var isPersonReport = IsPersonReport(report.Type);

            if (image != null)
            {
                try
                {
                    if (isPersonReport)
                    {
                        var faceResults = await _aiService.FaceMatchAsync(image, k);
                        if (faceResults.Count > 0)
                            return faceResults;

                        return await _aiService.SearchTextAsync(text, k);
                    }

                    return await _aiService.MultiModalSearchAsync(text, image, k);
                }
                finally
                {
                    imageStream?.Dispose();
                }
            }

            return await _aiService.SearchTextAsync(text, k);
        }

        private static bool IsPersonReport(ReportType reportType)
        {
            return reportType is ReportType.LostPerson or ReportType.FoundPerson;
        }

        private static bool TryGetAiResultId(AiResultDto result, bool isPersonReport, out int id)
        {
            if (isPersonReport)
            {
                var personOrFallbackId = !string.IsNullOrWhiteSpace(result.PersonId)
                    ? result.PersonId
                    : result.PostId;

                return int.TryParse(personOrFallbackId, out id);
            }

            return int.TryParse(result.PostId, out id);
        }

        private static (IFormFile? File, Stream? Stream) TryOpenFirstReportImage(Report report)
        {
            var firstImageUrl = report.Images?.FirstOrDefault()?.ImageUrl;
            if (string.IsNullOrWhiteSpace(firstImageUrl))
                return (null, null);

            try
            {
                var relativePath = firstImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
                if (!File.Exists(fullPath))
                    return (null, null);

                var stream = File.OpenRead(fullPath);
                var fileName = Path.GetFileName(fullPath);
                var file = new FormFile(stream, 0, stream.Length, "image", fileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/octet-stream"
                };
                return (file, stream);
            }
            catch
            {
                return (null, null);
            }
        }

        private async Task UpsertMatchAndMaybeNotifyAsync(
            Report report,
            Report candidate,
            double score,
            List<ReportMatchDto> results)
        {
            var exists = await _unitOfWork.ReportMatches.ExistsAsync(m =>
                m.ReportId == report.Id && m.MatchedReportId == candidate.Id);

            if (!exists)
            {
                var match = new ReportMatch
                {
                    ReportId = report.Id,
                    MatchedReportId = candidate.Id,
                    SimilarityScore = score,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.ReportMatches.AddAsync(match);
            }

            if (score >= 80)
            {
                if (report.Status == ReportStatus.Open)
                {
                    report.Status = ReportStatus.Matched;
                    report.MatchPercentage = score;
                    await _unitOfWork.Reports.UpdateAsync(report);
                }

                try
                {
                    await _notificationService.NotifyReportMatchAsync(report, candidate, score);
                }
                catch { /* Don't fail matching if notification fails */ }
            }

            results.Add(new ReportMatchDto
            {
                ReportId = report.Id,
                MatchedReportId = candidate.Id,
                MatchedReportTitle = candidate.Title,
                SimilarityScore = score,
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task<List<ReportMatchDto>> GetMatchesAsync(int reportId)
        {
            var matches = await _unitOfWork.ReportMatches.GetQueryable()
                .Include(m => m.MatchedReport)
                .Where(m => m.ReportId == reportId)
                .OrderByDescending(m => m.SimilarityScore)
                .ToListAsync();

            return matches.Select(m => new ReportMatchDto
            {
                Id = m.Id,
                ReportId = m.ReportId,
                MatchedReportId = m.MatchedReportId,
                MatchedReportTitle = m.MatchedReport?.Title,
                SimilarityScore = m.SimilarityScore,
                CreatedAt = m.CreatedAt
            }).ToList();
        }

        /// <summary>
        /// Mock AI similarity calculation.
        /// Combines subcategory match, location proximity, and keyword overlap.
        /// </summary>
        private static double CalculateSimilarity(Report source, Report candidate)
        {
            double score = 0;

            // 40 points for same subcategory (guaranteed by query, but kept for clarity)
            if (source.SubCategoryId == candidate.SubCategoryId)
                score += 40;

            // 30 points for location proximity
            if (source.Latitude.HasValue && source.Longitude.HasValue &&
                candidate.Latitude.HasValue && candidate.Longitude.HasValue)
            {
                var distance = HaversineKm(source.Latitude.Value, source.Longitude.Value,
                    candidate.Latitude.Value, candidate.Longitude.Value);
                if (distance <= 1) score += 30;
                else if (distance <= 5) score += 20;
                else if (distance <= 20) score += 10;
            }

            // 30 points for keyword overlap in title/description
            var sourceWords = $"{source.Title} {source.Description}"
                .ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3).ToHashSet();
            var candidateWords = $"{candidate.Title} {candidate.Description}"
                .ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3).ToHashSet();

            if (sourceWords.Count > 0 && candidateWords.Count > 0)
            {
                var overlap = sourceWords.Intersect(candidateWords).Count();
                var overlapRatio = (double)overlap / Math.Min(sourceWords.Count, candidateWords.Count);
                score += overlapRatio * 30;
            }

            return Math.Min(100, Math.Round(score, 1));
        }

        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            var dLat = (lat2 - lat1) * Math.PI / 180.0;
            var dLon = (lon2 - lon1) * Math.PI / 180.0;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}
