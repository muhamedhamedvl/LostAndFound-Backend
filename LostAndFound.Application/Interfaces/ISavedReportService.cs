using System.Collections.Generic;
using System.Threading.Tasks;
using LostAndFound.Application.DTOs.Report;

namespace LostAndFound.Application.Interfaces
{
    public interface ISavedReportService
    {
        Task<bool> SaveReportAsync(int reportId, int userId);
        Task<bool> UnsaveReportAsync(int reportId, int userId);
        Task<List<ReportDto>> GetSavedReportsAsync(int userId);
    }
}

