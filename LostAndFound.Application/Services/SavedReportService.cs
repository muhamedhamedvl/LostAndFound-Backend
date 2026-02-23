using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LostAndFound.Application.DTOs.Report;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Application.Services
{
    public class SavedReportService : ISavedReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReportService _reportService;

        public SavedReportService(IUnitOfWork unitOfWork, IReportService reportService)
        {
            _unitOfWork = unitOfWork;
            _reportService = reportService;
        }

        public async Task<bool> SaveReportAsync(int reportId, int userId)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(reportId);
            if (report == null)
            {
                return false;
            }

            var exists = await _unitOfWork.SavedReports
                .GetQueryable()
                .AnyAsync(x => x.ReportId == reportId && x.UserId == userId);

            if (exists)
            {
                // Already saved
                return false;
            }

            var saved = new SavedReport
            {
                ReportId = reportId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.SavedReports.AddAsync(saved);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UnsaveReportAsync(int reportId, int userId)
        {
            var existing = await _unitOfWork.SavedReports
                .GetQueryable()
                .FirstOrDefaultAsync(x => x.ReportId == reportId && x.UserId == userId);

            if (existing == null)
            {
                return false;
            }

            await _unitOfWork.SavedReports.DeleteAsync(existing);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<List<ReportDto>> GetSavedReportsAsync(int userId)
        {
            var savedIds = await _unitOfWork.SavedReports
                .GetQueryable()
                .Where(x => x.UserId == userId)
                .Select(x => x.ReportId)
                .ToListAsync();

            var reports = new List<ReportDto>();
            foreach (var id in savedIds)
            {
                var report = await _reportService.GetByIdAsync(id, userId, isAdmin: false);
                if (report != null)
                {
                    reports.Add(report);
                }
            }

            return reports;
        }
    }
}

