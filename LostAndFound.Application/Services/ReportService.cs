using AutoMapper;
using LostAndFound.Application.DTOs.Report;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LostAndFound.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IImageService _imageService;
        private readonly IServiceScopeFactory _scopeFactory;

        public ReportService(IUnitOfWork unitOfWork, IMapper mapper, IImageService imageService, IServiceScopeFactory scopeFactory)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _imageService = imageService;
            _scopeFactory = scopeFactory;
        }

        private IQueryable<Report> ReportsWithIncludes()
        {
            return _unitOfWork.Reports.GetQueryable()
                .Include(r => r.SubCategory)
                    .ThenInclude(sc => sc.Category)
                .Include(r => r.CreatedBy)
                .Include(r => r.Images);
        }

        public async Task<ReportDto> CreateAsync(CreateReportDto dto, int userId)
        {
            // Validate and parse the Type before mapping
            if (!Enum.TryParse<ReportType>(dto.Type, true, out var reportType))
            {
                throw new ArgumentException($"Invalid report type '{dto.Type}'. Valid types are: {string.Join(", ", Enum.GetNames<ReportType>())}");
            }

            var report = new Report
            {
                Title = dto.Title,
                Description = dto.Description,
                Type = reportType,
                Status = ReportStatus.Open,
                LifecycleStatus = ReportLifecycleStatus.Pending,
                LocationName = dto.LocationName,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                SubCategoryId = dto.SubCategoryId,
                DateReported = dto.DateReported,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Reports.AddAsync(report);
            await _unitOfWork.SaveChangesAsync();

            // Handle images via IImageService
            const long reportImageMaxBytes = 10 * 1024 * 1024;
            if (dto.Images != null && dto.Images.Count > 0)
            {
                var subFolder = $"reports/{report.Id}";
                foreach (var imageFile in dto.Images)
                {
                    if (imageFile == null || imageFile.Length == 0) continue;

                    var (isValid, _) = _imageService.ValidateImage(imageFile, reportImageMaxBytes);
                    if (!isValid) continue;

                    var imageUrl = await _imageService.SaveImageAsync(imageFile, subFolder, reportImageMaxBytes);
                    var reportImage = new ReportImage
                    {
                        ReportId = report.Id,
                        ImageUrl = imageUrl,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.ReportImages.AddAsync(reportImage);
                }
                await _unitOfWork.SaveChangesAsync();
            }

            var created = await ReportsWithIncludes().FirstOrDefaultAsync(r => r.Id == report.Id);

            // Fire-and-forget: trigger matching in background (non-blocking, new scope for scoped services)
            var reportId = report.Id;
            _ = Task.Run(async () =>
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var matchingService = scope.ServiceProvider.GetRequiredService<IMatchingService>();
                    await matchingService.RunMatchingAsync(reportId);
                }
                catch { /* Don't fail report creation; matching is best-effort */ }
            });

            return _mapper.Map<ReportDto>(created);
        }

        public async Task<ReportDto?> GetByIdAsync(int id, int? requesterUserId = null, bool isAdmin = false)
        {
            var report = await ReportsWithIncludes().FirstOrDefaultAsync(r => r.Id == id);
            if (report == null) return null;

            // Owners and admins can access any report
            var isOwner = requesterUserId.HasValue && report.CreatedById == requesterUserId.Value;
            if (isOwner || isAdmin)
                return _mapper.Map<ReportDto>(report);

            // Anonymous or other users: only Approved/Matched/Closed
            if (report.LifecycleStatus != ReportLifecycleStatus.Approved
                && report.LifecycleStatus != ReportLifecycleStatus.Matched
                && report.LifecycleStatus != ReportLifecycleStatus.Closed)
                return null;

            return _mapper.Map<ReportDto>(report);
        }

        public async Task<(List<ReportDto> Reports, int TotalCount)> GetAllAsync(ReportFilterDto filter)
        {
            filter.Page = Math.Max(1, filter.Page);
            filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);

            var query = ReportsWithIncludes();

            if (!string.IsNullOrEmpty(filter.Type) && Enum.TryParse<ReportType>(filter.Type, true, out var reportType))
                query = query.Where(r => r.Type == reportType);

            if (!string.IsNullOrEmpty(filter.Status))
            {
                // First try lifecycle status, then fall back to legacy status for backward compatibility
                if (Enum.TryParse<ReportLifecycleStatus>(filter.Status, true, out var lifecycleStatus))
                {
                    query = query.Where(r => r.LifecycleStatus == lifecycleStatus);
                }
                else if (Enum.TryParse<ReportStatus>(filter.Status, true, out var reportStatus))
                {
                    query = query.Where(r => r.Status == reportStatus);
                }
            }
            else if (filter.ForPublicView)
            {
                // Public view: only show Approved, Matched, Closed (hide Pending, Rejected, Flagged, Archived)
                query = query.Where(r => r.LifecycleStatus == ReportLifecycleStatus.Approved
                    || r.LifecycleStatus == ReportLifecycleStatus.Matched
                    || r.LifecycleStatus == ReportLifecycleStatus.Closed);
            }

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(r => r.Title.Contains(filter.Search) || r.Description.Contains(filter.Search) || (r.LocationName != null && r.LocationName.Contains(filter.Search)));

            if (filter.CategoryId.HasValue)
                query = query.Where(r => r.SubCategory != null && r.SubCategory.CategoryId == filter.CategoryId.Value);

            if (filter.SubCategoryId.HasValue)
                query = query.Where(r => r.SubCategoryId == filter.SubCategoryId.Value);

            if (filter.DateFrom.HasValue)
                query = query.Where(r => r.CreatedAt >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(r => r.CreatedAt <= filter.DateTo.Value);

            var totalCount = await query.CountAsync();
            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (_mapper.Map<List<ReportDto>>(reports), totalCount);
        }

        public async Task<(List<ReportDto> Reports, int TotalCount)> GetMyReportsAsync(int userId, int page, int pageSize)
        {
            return await GetReportsByUserIdAsync(userId, page, pageSize);
        }

        public async Task<(List<ReportDto> Reports, int TotalCount)> GetReportsByUserIdAsync(int userId, int page, int pageSize)
        {
            var query = ReportsWithIncludes().Where(r => r.CreatedById == userId);
            var totalCount = await query.CountAsync();
            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (_mapper.Map<List<ReportDto>>(reports), totalCount);
        }

        public async Task<List<NearbyReportDto>> GetNearbyAsync(double latitude, double longitude, double radiusKm, string? type, int page, int pageSize)
        {
            var query = ReportsWithIncludes()
                .Where(r => r.Latitude.HasValue && r.Longitude.HasValue)
                // Public endpoint: only show approved reports
                .Where(r => r.LifecycleStatus == ReportLifecycleStatus.Approved
                    || r.LifecycleStatus == ReportLifecycleStatus.Matched
                    || r.LifecycleStatus == ReportLifecycleStatus.Closed);

            // Filter by type if provided (Lost or Found)
            if (!string.IsNullOrEmpty(type) && !string.Equals(type, "All", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(type, "Lost", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.Type == ReportType.LostItem || r.Type == ReportType.LostPerson);
                }
                else if (string.Equals(type, "Found", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.Type == ReportType.FoundItem || r.Type == ReportType.FoundPerson);
                }
            }

            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var filtered = reports
                .Where(r => HaversineKm(latitude, longitude, r.Latitude!.Value, r.Longitude!.Value) <= radiusKm)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return filtered.Select(r => new NearbyReportDto
            {
                Id = r.Id,
                Title = r.Title,
                Type = r.Type.ToString().StartsWith("Lost") ? "Lost" : "Found",
                Category = r.SubCategory?.Category?.Name ?? string.Empty,
                SubCategory = r.SubCategory?.Name ?? string.Empty,
                Lat = r.Latitude!.Value,
                Lng = r.Longitude!.Value,
                ImageUrl = r.Images?.FirstOrDefault()?.ImageUrl
            }).ToList();
        }

        public async Task<ReportDto?> UpdateAsync(int id, UpdateReportDto dto, int userId, bool isAdmin)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(id);
            if (report == null) return null;
            if (report.CreatedById != userId && !isAdmin) return null;

            if (dto.Title != null) report.Title = dto.Title;
            if (dto.Description != null) report.Description = dto.Description;

            if (!string.IsNullOrEmpty(dto.Type) && Enum.TryParse<ReportType>(dto.Type, true, out var rt))
                report.Type = rt;

            if (dto.LocationName != null) report.LocationName = dto.LocationName;
            report.Latitude = dto.Latitude;
            report.Longitude = dto.Longitude;
            if (dto.SubCategoryId.HasValue) report.SubCategoryId = dto.SubCategoryId.Value;
            if (dto.DateReported.HasValue) report.DateReported = dto.DateReported;
            report.UpdatedAt = DateTime.UtcNow;

            // Remove images
            if (dto.ImageIdsToRemove != null && dto.ImageIdsToRemove.Count > 0)
            {
                var images = await _unitOfWork.ReportImages.FindAsync(i => dto.ImageIdsToRemove.Contains(i.Id) && i.ReportId == id);
                foreach (var img in images)
                {
                    await _imageService.DeleteImageAsync(img.ImageUrl);
                    await _unitOfWork.ReportImages.DeleteAsync(img);
                }
            }

            // Add new images via IImageService
            const long reportImageMaxBytes = 10 * 1024 * 1024;
            if (dto.NewImages != null && dto.NewImages.Count > 0)
            {
                var subFolder = $"reports/{id}";
                foreach (var imageFile in dto.NewImages)
                {
                    if (imageFile == null || imageFile.Length == 0) continue;

                    var (isValid, _) = _imageService.ValidateImage(imageFile, reportImageMaxBytes);
                    if (!isValid) continue;

                    var imageUrl = await _imageService.SaveImageAsync(imageFile, subFolder, reportImageMaxBytes);
                    await _unitOfWork.ReportImages.AddAsync(new ReportImage
                    {
                        ReportId = id,
                        ImageUrl = imageUrl,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _unitOfWork.Reports.UpdateAsync(report);
            await _unitOfWork.SaveChangesAsync();

            var updated = await ReportsWithIncludes().FirstOrDefaultAsync(r => r.Id == id);
            return _mapper.Map<ReportDto>(updated);
        }

        public async Task<bool> DeleteAsync(int id, int userId, bool isAdmin)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(id);
            if (report == null) return false;
            if (report.CreatedById != userId && !isAdmin) return false;

            // Clean up images from disk
            var images = await _unitOfWork.ReportImages.FindAsync(i => i.ReportId == id);
            foreach (var img in images)
            {
                await _imageService.DeleteImageAsync(img.ImageUrl);
            }

            await _unitOfWork.Reports.DeleteAsync(report);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<ReportDto?> UpdateStatusAsync(int id, string status, int userId, bool isAdmin)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(id);
            if (report == null) return null;
            if (report.CreatedById != userId && !isAdmin) return null;

            if (!Enum.TryParse<ReportLifecycleStatus>(status, true, out var newLifecycleStatus))
                return null;

            // C3 fix: Non-admin users can only Close their own reports (withdraw).
            // Admin-only transitions: Approved, Rejected, Flagged, Archived, Matched.
            if (!isAdmin)
            {
                var current = report.LifecycleStatus;
                var allowed = current switch
                {
                    // User can't change Pending — must wait for admin approval
                    ReportLifecycleStatus.Pending => Array.Empty<ReportLifecycleStatus>(),
                    // User can close (withdraw) their approved report
                    ReportLifecycleStatus.Approved => new[] { ReportLifecycleStatus.Closed },
                    // User can close a matched report
                    ReportLifecycleStatus.Matched => new[] { ReportLifecycleStatus.Closed },
                    _ => Array.Empty<ReportLifecycleStatus>()
                };

                if (!allowed.Contains(newLifecycleStatus))
                {
                    return null;
                }
            }

            report.LifecycleStatus = newLifecycleStatus;

            // Keep legacy Status roughly in sync for backward compatibility
            report.Status = newLifecycleStatus switch
            {
                ReportLifecycleStatus.Matched => ReportStatus.Matched,
                ReportLifecycleStatus.Closed or ReportLifecycleStatus.Archived => ReportStatus.Closed,
                _ => ReportStatus.Open
            };

            report.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Reports.UpdateAsync(report);
            await _unitOfWork.SaveChangesAsync();

            var updated = await ReportsWithIncludes().FirstOrDefaultAsync(r => r.Id == id);
            return _mapper.Map<ReportDto>(updated);
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
