using LostAndFound.Application.DTOs.Report;
using LostAndFound.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Application.Services
{
    public class HomeService : IHomeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReportService _reportService;

        public HomeService(IUnitOfWork unitOfWork, IReportService reportService)
        {
            _unitOfWork = unitOfWork;
            _reportService = reportService;
        }

        public async Task<HomeDashboardDto> GetDashboardAsync(int? userId = null)
        {
            var filter = new ReportFilterDto
            {
                Page = 1,
                PageSize = 10,
                ForPublicView = true // Dashboard shows only approved reports
            };
            var (recentReports, totalReportsCount) = await _reportService.GetAllAsync(filter);

            var categoriesCount = await _unitOfWork.Categories.GetQueryable().CountAsync();

            int? myReportsCount = null;
            if (userId.HasValue)
            {
                var (_, count) = await _reportService.GetReportsByUserIdAsync(userId.Value, 1, 1);
                myReportsCount = count;
            }

            return new HomeDashboardDto
            {
                RecentReports = recentReports,
                TotalReportsCount = totalReportsCount,
                CategoriesCount = categoriesCount,
                MyReportsCount = myReportsCount
            };
        }
    }
}
