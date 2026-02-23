using LostAndFound.Application.DTOs.Report;
using LostAndFound.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LostAndFound.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("api")]
    public class AdminController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public AdminController(IReportService reportService, IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _reportService = reportService;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Get all reports (admin view with all statuses).
        /// </summary>
        [HttpGet("reports")]
        public async Task<IActionResult> GetAllReports([FromQuery] ReportFilterDto filter)
        {
            // Admin view: no lifecycle restriction, see all reports
            filter.ForPublicView = false;
            var (reports, totalCount) = await _reportService.GetAllAsync(filter);
            return Ok(new
            {
                data = reports,
                totalCount,
                page = filter.Page,
                pageSize = filter.PageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            });
        }

        /// <summary>
        /// Approve a report (set lifecycle to Approved).
        /// </summary>
        [HttpPut("reports/{id}/approve")]
        public async Task<IActionResult> ApproveReport(int id)
        {
            var result = await _reportService.UpdateStatusAsync(id, "Approved", 0, isAdmin: true);
            if (result == null)
                return NotFound(new { message = "Report not found or invalid status transition" });

            await _notificationService.NotifyReportStatusChangeAsync(id, "Approved");
            return Ok(result);
        }

        /// <summary>
        /// Reject a report (set lifecycle to Rejected).
        /// </summary>
        [HttpPut("reports/{id}/reject")]
        public async Task<IActionResult> RejectReport(int id)
        {
            var result = await _reportService.UpdateStatusAsync(id, "Rejected", 0, isAdmin: true);
            if (result == null)
                return NotFound(new { message = "Report not found or invalid status transition" });

            await _notificationService.NotifyReportStatusChangeAsync(id, "Rejected");
            return Ok(result);
        }

        /// <summary>
        /// Flag a report for review (set lifecycle to Flagged).
        /// </summary>
        [HttpPut("reports/{id}/flag")]
        public async Task<IActionResult> FlagReport(int id)
        {
            var result = await _reportService.UpdateStatusAsync(id, "Flagged", 0, isAdmin: true);
            if (result == null)
                return NotFound(new { message = "Report not found or invalid status transition" });

            await _notificationService.NotifyReportStatusChangeAsync(id, "Flagged");
            return Ok(result);
        }

        /// <summary>
        /// Archive a report (set lifecycle to Archived).
        /// </summary>
        [HttpPut("reports/{id}/archive")]
        public async Task<IActionResult> ArchiveReport(int id)
        {
            var result = await _reportService.UpdateStatusAsync(id, "Archived", 0, isAdmin: true);
            if (result == null)
                return NotFound(new { message = "Report not found or invalid status transition" });

            await _notificationService.NotifyReportStatusChangeAsync(id, "Archived");
            return Ok(result);
        }

        /// <summary>
        /// Delete any report (admin privilege).
        /// </summary>
        [HttpDelete("reports/{id}")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var deleted = await _reportService.DeleteAsync(id, 0, isAdmin: true);
            if (!deleted)
                return NotFound(new { message = "Report not found" });
            return NoContent();
        }

        /// <summary>
        /// Verify a user account manually.
        /// </summary>
        [HttpPut("users/{id}/verify")]
        public async Task<IActionResult> VerifyUser(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.IsVerified = true;
            user.VerificationCode = null;
            user.VerificationCodeExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = $"User {user.FullName} has been verified" });
        }
    }
}
