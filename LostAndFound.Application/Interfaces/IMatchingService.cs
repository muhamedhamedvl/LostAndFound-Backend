using LostAndFound.Application.DTOs.Report;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LostAndFound.Application.Interfaces
{
    public interface IMatchingService
    {
        Task<List<ReportMatchDto>> RunMatchingAsync(int reportId);
        Task<List<ReportMatchDto>> GetMatchesAsync(int reportId);
    }
}
