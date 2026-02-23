using System.Threading.Tasks;
using LostAndFound.Application.DTOs.Report;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Application.Services
{
    public class ReportAbuseService : IReportAbuseService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportAbuseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> ReportAbuseAsync(int reportId, int reporterId, ReportAbuseDto dto)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(reportId);
            if (report == null)
            {
                return false;
            }

            if (report.CreatedById == reporterId)
            {
                // Cannot report own report
                return false;
            }

            var exists = await _unitOfWork.ReportAbuses
                .GetQueryable()
                .AnyAsync(x => x.ReportId == reportId && x.ReporterId == reporterId);

            if (exists)
            {
                // Duplicate report
                return false;
            }

            var abuse = new ReportAbuse
            {
                ReportId = reportId,
                ReporterId = reporterId,
                Reason = dto.Reason,
                Details = dto.Details,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.ReportAbuses.AddAsync(abuse);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}

