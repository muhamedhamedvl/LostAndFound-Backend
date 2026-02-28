using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Report;
using LostAndFound.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace LostAndFound.Api.Controllers
{
    /// <summary>
    /// Administrative endpoints for report moderation and user management. Requires Admin role.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("api")]
    public class AdminController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IReportService reportService,
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            ILogger<AdminController> logger)
        {
            _reportService = reportService;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Get all reports for moderation
        /// </summary>
        /// <remarks>
        /// Returns all reports across all lifecycle statuses (Pending, Approved, Rejected, Flagged, Matched, Closed, Archived).
        /// Supports filtering by type, status, category, date range, and free-text search. Results are paginated.
        /// Requires Admin role.
        /// </remarks>
        /// <param name="filter">Filter and pagination parameters</param>
        /// <response code="200">Reports retrieved successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User does not have Admin role</response>
        [HttpGet("reports")]
        [SwaggerOperation(
            Summary = "Get all reports for moderation",
            Description = "Returns all reports across all lifecycle statuses with filtering and pagination. Requires Admin role."
        )]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllReports([FromQuery] ReportFilterDto filter)
        {
            try
            {
                // Admin view: no lifecycle restriction, see all reports
                filter.ForPublicView = false;
                var (reports, totalCount) = await _reportService.GetAllAsync(filter);
                var payload = new
                {
                    data = reports,
                    totalCount,
                    page = filter.Page,
                    pageSize = filter.PageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
                };
                return Ok(BaseResponse<object>.SuccessResult(payload, "Reports retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin reports");
                return StatusCode(500, BaseResponse<object>.FailureResult("An unexpected error occurred."));
            }
        }

        /// <summary>
        /// Approve a report
        /// </summary>
        /// <remarks>
        /// Sets the report lifecycle status to Approved, making it publicly visible to all users.
        /// The report owner is notified of the status change. Requires Admin role.
        /// </remarks>
        /// <param name="id">The report ID to approve</param>
        /// <response code="200">Report approved successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User does not have Admin role</response>
        /// <response code="404">Report not found or invalid status transition</response>
        [HttpPut("reports/{id}/approve")]
        [SwaggerOperation(
            Summary = "Approve a report",
            Description = "Approve a report and make it publicly visible. The report owner is notified. Requires Admin role."
        )]
        [ProducesResponseType(typeof(BaseResponse<ReportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<ReportDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveReport(int id)
        {
            try
            {
                var result = await _reportService.UpdateStatusAsync(id, "Approved", 0, isAdmin: true);
                if (result == null)
                    return NotFound(BaseResponse<ReportDto>.FailureResult("Report not found or invalid status transition"));

                await _notificationService.NotifyReportStatusChangeAsync(id, "Approved");
                return Ok(BaseResponse<ReportDto>.SuccessResult(result, "Report approved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving report {ReportId}", id);
                return StatusCode(500, BaseResponse<object>.FailureResult("An unexpected error occurred."));
            }
        }

        /// <summary>
        /// Reject a report
        /// </summary>
        /// <remarks>
        /// Sets the report lifecycle status to Rejected due to policy violation or invalid data.
        /// The report is hidden from public view. The report owner is notified. Requires Admin role.
        /// </remarks>
        /// <param name="id">The report ID to reject</param>
        /// <response code="200">Report rejected successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User does not have Admin role</response>
        /// <response code="404">Report not found or invalid status transition</response>
        [HttpPut("reports/{id}/reject")]
        [SwaggerOperation(
            Summary = "Reject a report",
            Description = "Reject a report due to policy violation or invalid data. The report owner is notified. Requires Admin role."
        )]
        [ProducesResponseType(typeof(BaseResponse<ReportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<ReportDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectReport(int id)
        {
            try
            {
                var result = await _reportService.UpdateStatusAsync(id, "Rejected", 0, isAdmin: true);
                if (result == null)
                    return NotFound(BaseResponse<ReportDto>.FailureResult("Report not found or invalid status transition"));

                await _notificationService.NotifyReportStatusChangeAsync(id, "Rejected");
                return Ok(BaseResponse<ReportDto>.SuccessResult(result, "Report rejected successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting report {ReportId}", id);
                return StatusCode(500, BaseResponse<object>.FailureResult("An unexpected error occurred."));
            }
        }

        /// <summary>
        /// Flag a report for review
        /// </summary>
        /// <remarks>
        /// Sets the report lifecycle status to Flagged, marking it as suspicious or requiring further review.
        /// The report is hidden from public view. The report owner is notified. Requires Admin role.
        /// </remarks>
        /// <param name="id">The report ID to flag</param>
        /// <response code="200">Report flagged successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User does not have Admin role</response>
        /// <response code="404">Report not found or invalid status transition</response>
        [HttpPut("reports/{id}/flag")]
        [SwaggerOperation(
            Summary = "Flag a report for review",
            Description = "Flag a report for further review or mark as suspicious. The report owner is notified. Requires Admin role."
        )]
        [ProducesResponseType(typeof(BaseResponse<ReportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<ReportDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> FlagReport(int id)
        {
            try
            {
                var result = await _reportService.UpdateStatusAsync(id, "Flagged", 0, isAdmin: true);
                if (result == null)
                    return NotFound(BaseResponse<ReportDto>.FailureResult("Report not found or invalid status transition"));

                await _notificationService.NotifyReportStatusChangeAsync(id, "Flagged");
                return Ok(BaseResponse<ReportDto>.SuccessResult(result, "Report flagged successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flagging report {ReportId}", id);
                return StatusCode(500, BaseResponse<object>.FailureResult("An unexpected error occurred."));
            }
        }

        /// <summary>
        /// Archive a report
        /// </summary>
        /// <remarks>
        /// Sets the report lifecycle status to Archived, soft-removing it from the active reports list.
        /// The report is hidden from public view but retained in the database. The report owner is notified. Requires Admin role.
        /// </remarks>
        /// <param name="id">The report ID to archive</param>
        /// <response code="200">Report archived successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User does not have Admin role</response>
        /// <response code="404">Report not found or invalid status transition</response>
        [HttpPut("reports/{id}/archive")]
        [SwaggerOperation(
            Summary = "Archive a report",
            Description = "Archive a report to soft-remove it from the active list. The report owner is notified. Requires Admin role."
        )]
        [ProducesResponseType(typeof(BaseResponse<ReportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<ReportDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ArchiveReport(int id)
        {
            try
            {
                var result = await _reportService.UpdateStatusAsync(id, "Archived", 0, isAdmin: true);
                if (result == null)
                    return NotFound(BaseResponse<ReportDto>.FailureResult("Report not found or invalid status transition"));

                await _notificationService.NotifyReportStatusChangeAsync(id, "Archived");
                return Ok(BaseResponse<ReportDto>.SuccessResult(result, "Report archived successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving report {ReportId}", id);
                return StatusCode(500, BaseResponse<object>.FailureResult("An unexpected error occurred."));
            }
        }

        /// <summary>
        /// Delete a report permanently
        /// </summary>
        /// <remarks>
        /// Permanently deletes a report and its associated images from the system.
        /// This action cannot be undone. Requires Admin role.
        /// </remarks>
        /// <param name="id">The report ID to delete</param>
        /// <response code="204">Report deleted successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User does not have Admin role</response>
        /// <response code="404">Report not found</response>
        [HttpDelete("reports/{id}")]
        [SwaggerOperation(
            Summary = "Delete a report permanently",
            Description = "Permanently delete a report and its associated images from the system. This action cannot be undone. Requires Admin role."
        )]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteReport(int id)
        {
            try
            {
                var deleted = await _reportService.DeleteAsync(id, 0, isAdmin: true);
                if (!deleted)
                    return NotFound(BaseResponse<object>.FailureResult("Report not found"));
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report {ReportId}", id);
                return StatusCode(500, BaseResponse<object>.FailureResult("An unexpected error occurred."));
            }
        }

        /// <summary>
        /// Verify a user account manually
        /// </summary>
        /// <remarks>
        /// Manually verifies a user account by setting IsVerified to true and clearing any pending verification codes.
        /// Use this when a user cannot complete email verification. Requires Admin role.
        /// </remarks>
        /// <param name="id">The user ID to verify</param>
        /// <response code="200">User verified successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User does not have Admin role</response>
        /// <response code="404">User not found</response>
        [HttpPut("users/{id}/verify")]
        [SwaggerOperation(
            Summary = "Verify a user account manually",
            Description = "Manually verify a user account by admin action. Use when a user cannot complete email verification. Requires Admin role."
        )]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerifyUser(int id)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                    return NotFound(BaseResponse<object>.FailureResult("User not found"));

                user.IsVerified = true;
                user.VerificationCode = null;
                user.VerificationCodeExpiry = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(BaseResponse<object>.SuccessResult(
                    new { userId = id, fullName = user.FullName },
                    $"User {user.FullName} has been verified"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying user {UserId}", id);
                return StatusCode(500, BaseResponse<object>.FailureResult("An unexpected error occurred."));
            }
        }
    }
}
