using AutoMapper;
using LostAndFound.Application.DTOs.Report;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using Microsoft.EntityFrameworkCore;

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

        public MatchingService(IUnitOfWork unitOfWork, IMapper mapper, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        public async Task<List<ReportMatchDto>> RunMatchingAsync(int reportId)
        {
            var report = await _unitOfWork.Reports.GetQueryable()
                .Include(r => r.SubCategory)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                throw new KeyNotFoundException("Report not found");

            // Find candidate reports: same subcategory, different user, not closed
            var candidates = await _unitOfWork.Reports.GetQueryable()
                .Include(r => r.SubCategory)
                .Where(r => r.Id != reportId
                         && r.CreatedById != report.CreatedById
                         && r.Status != ReportStatus.Closed
                         && r.SubCategoryId == report.SubCategoryId)
                .Take(100)
                .ToListAsync();

            var results = new List<ReportMatchDto>();

            foreach (var candidate in candidates)
            {
                var score = CalculateSimilarity(report, candidate);
                if (score < 30) continue; // Skip very low matches

                // Check if match already exists
                var exists = await _unitOfWork.ReportMatches.ExistsAsync(m =>
                    m.ReportId == reportId && m.MatchedReportId == candidate.Id);

                if (!exists)
                {
                    var match = new ReportMatch
                    {
                        ReportId = reportId,
                        MatchedReportId = candidate.Id,
                        SimilarityScore = score,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.ReportMatches.AddAsync(match);
                }

                // If high similarity, update status and notify
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
                    ReportId = reportId,
                    MatchedReportId = candidate.Id,
                    MatchedReportTitle = candidate.Title,
                    SimilarityScore = score,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _unitOfWork.SaveChangesAsync();
            return results.OrderByDescending(r => r.SimilarityScore).ToList();
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
