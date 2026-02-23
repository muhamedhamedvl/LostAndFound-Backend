using LostAndFound.Application.DTOs.Report;

namespace LostAndFound.Application.Interfaces
{
    public interface IHomeService
    {
        /// <summary>
        /// Gets dashboard data: recent reports, total reports count, categories count, and optionally user's reports count.
        /// </summary>
        Task<HomeDashboardDto> GetDashboardAsync(int? userId = null);
    }

    public class HomeDashboardDto
    {
        public List<ReportDto> RecentReports { get; set; } = new();
        public int TotalReportsCount { get; set; }
        public int CategoriesCount { get; set; }
        public int? MyReportsCount { get; set; }
    }
}
