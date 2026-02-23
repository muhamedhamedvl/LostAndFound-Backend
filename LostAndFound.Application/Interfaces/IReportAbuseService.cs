using System.Threading.Tasks;
using LostAndFound.Application.DTOs.Report;

namespace LostAndFound.Application.Interfaces
{
    public interface IReportAbuseService
    {
        Task<bool> ReportAbuseAsync(int reportId, int reporterId, ReportAbuseDto dto);
    }
}

