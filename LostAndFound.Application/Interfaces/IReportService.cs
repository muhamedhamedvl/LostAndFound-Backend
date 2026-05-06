using LostAndFound.Application.DTOs.Report;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LostAndFound.Application.Interfaces
{
    public interface IReportService
    {
        Task<ReportDto> CreateAsync(CreateReportDto dto, int userId);
        /// <summary>
        /// Gets a report by ID.
        /// </summary>
        Task<ReportDto?> GetByIdAsync(int id, int? requesterUserId = null, bool isAdmin = false);
        Task<(List<ReportDto> Reports, int TotalCount)> GetAllAsync(ReportFilterDto filter);
        Task<(List<ReportDto> Reports, int TotalCount)> GetMyReportsAsync(int userId, int page, int pageSize);
        Task<(List<ReportDto> Reports, int TotalCount)> GetReportsByUserIdAsync(int userId, int page, int pageSize);
        Task<List<NearbyReportDto>> GetNearbyAsync(double latitude, double longitude, double radiusKm, string? type, int page, int pageSize);
        Task<ReportDto?> UpdateAsync(int id, UpdateReportDto dto, int userId, bool isAdmin);
        Task<bool> DeleteAsync(int id, int userId, bool isAdmin);
        Task<ReportDto?> UpdateStatusAsync(int id, string status, int userId, bool isAdmin);
    }
}
