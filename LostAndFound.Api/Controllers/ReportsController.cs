using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Report;
using LostAndFound.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace LostAndFound.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("api")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly INotificationService _notificationService;
        private readonly IReportAbuseService _reportAbuseService;
        private readonly ISavedReportService _savedReportService;

        public ReportsController(
            IReportService reportService,
            INotificationService notificationService,
            IReportAbuseService reportAbuseService,
            ISavedReportService savedReportService)
        {
            _reportService = reportService;
            _notificationService = notificationService;
            _reportAbuseService = reportAbuseService;
            _savedReportService = savedReportService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private bool IsAdmin() => User.IsInRole("Admin");

        /// <summary>
        /// Create a new report with optional images.
        /// </summary>
        /// <remarks>
        /// Valid report types: LostItem, FoundItem, LostPerson, FoundPerson
        /// </remarks>
        [HttpPost]
        [EnableRateLimiting("upload")]
        public async Task<IActionResult> Create([FromForm] CreateReportDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(BaseResponse<ReportDto>.FailureResult("Validation failed", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            try
            {
                var result = await _reportService.CreateAsync(dto, GetUserId());
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, BaseResponse<ReportDto>.SuccessResult(result, "Report created successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse<ReportDto>.FailureResult(ex.Message));
            }
        }

        /// <summary>
        /// Get a single report by ID.
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            int? requesterUserId = User.Identity?.IsAuthenticated == true && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : null;
            var report = await _reportService.GetByIdAsync(id, requesterUserId, IsAdmin());
            if (report == null) return NotFound(BaseResponse<ReportDto>.FailureResult("Report not found"));
            return Ok(BaseResponse<ReportDto>.SuccessResult(report, "Report retrieved successfully"));
        }

        /// <summary>
        /// Express interest in a report. Notifies the report owner.
        /// </summary>
        [HttpPost("{id}/interested")]
        public async Task<IActionResult> ExpressInterest(int id)
        {
            var report = await _reportService.GetByIdAsync(id, GetUserId(), IsAdmin());
            if (report == null) return NotFound(BaseResponse<object>.FailureResult("Report not found"));

            var userId = GetUserId();
            if (report.CreatedById == userId)
                return BadRequest(BaseResponse<object>.FailureResult("You cannot express interest in your own report"));

            await _notificationService.NotifyInterestedInReportAsync(id, userId);
            return Ok(BaseResponse<object>.SuccessResult(new { reportId = id }, "Interest expressed successfully. The report owner has been notified."));
        }

        /// <summary>
        /// Get all reports with filtering and pagination.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] ReportFilterDto filter)
        {
            // Public view: only Approved/Matched/Closed (set server-side, never trust client)
            filter.ForPublicView = true;
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

        /// <summary>
        /// Get the current user's reports.
        /// </summary>
        [HttpGet("my-reports")]
        public async Task<IActionResult> GetMyReports([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var (reports, totalCount) = await _reportService.GetMyReportsAsync(GetUserId(), page, pageSize);
            var payload = new
            {
                data = reports,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
            return Ok(BaseResponse<object>.SuccessResult(payload, "Reports retrieved successfully"));
        }

        /// <summary>
        /// Get nearby reports using geo-coordinates.
        /// </summary>
        /// <param name="lat">Latitude (-90 to 90)</param>
        /// <param name="lng">Longitude (-180 to 180)</param>
        /// <param name="radius">Search radius in kilometers (default: 10)</param>
        /// <param name="type">Filter by type: Lost, Found, or All (default: All)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        [HttpGet("nearby")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNearby(
            [FromQuery] double lat,
            [FromQuery] double lng,
            [FromQuery] double radius = 10,
            [FromQuery] string? type = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (lat < -90 || lat > 90 || lng < -180 || lng > 180)
                return BadRequest(BaseResponse<object>.FailureResult("Invalid coordinates"));

            var reports = await _reportService.GetNearbyAsync(lat, lng, radius, type, page, pageSize);
            return Ok(BaseResponse<List<NearbyReportDto>>.SuccessResult(reports, "Nearby reports retrieved successfully"));
        }

        /// <summary>
        /// Update a report.
        /// </summary>
        [HttpPut("{id}")]
        [EnableRateLimiting("upload")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateReportDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(BaseResponse<ReportDto>.FailureResult("Validation failed", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            var result = await _reportService.UpdateAsync(id, dto, GetUserId(), IsAdmin());
            if (result == null) return NotFound(BaseResponse<ReportDto>.FailureResult("Report not found or unauthorized"));
            return Ok(BaseResponse<ReportDto>.SuccessResult(result, "Report updated successfully"));
        }

        /// <summary>
        /// Update report status.
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateReportStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(BaseResponse<ReportDto>.FailureResult("Validation failed"));

            var result = await _reportService.UpdateStatusAsync(id, dto.Status, GetUserId(), IsAdmin());
            if (result == null) return NotFound(BaseResponse<ReportDto>.FailureResult("Report not found or invalid status"));

            await _notificationService.NotifyReportStatusChangeAsync(id, dto.Status);
            return Ok(BaseResponse<ReportDto>.SuccessResult(result, "Status updated successfully"));
        }

        /// <summary>
        /// Delete a report.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _reportService.DeleteAsync(id, GetUserId(), IsAdmin());
            if (!deleted) return NotFound(BaseResponse<object>.FailureResult("Report not found or unauthorized"));
            return NoContent();
        }

        /// <summary>
        /// Report abuse for a specific report. Cannot report your own report and cannot create duplicates.
        /// </summary>
        [HttpPost("{id}/report")]
        public async Task<IActionResult> ReportAbuse(int id, [FromBody] ReportAbuseDto dto)
        {
            var userId = GetUserId();
            var success = await _reportAbuseService.ReportAbuseAsync(id, userId, dto);

            if (!success)
            {
                return BadRequest(BaseResponse<object>.FailureResult("Unable to report abuse. You may be reporting your own report or have already reported this report."));
            }

            return Ok(BaseResponse<object>.SuccessResult(new { reportId = id }, "Report has been flagged for review"));
        }

        /// <summary>
        /// Save a report for quick access.
        /// </summary>
        [HttpPost("{id}/save")]
        public async Task<IActionResult> SaveReport(int id)
        {
            var userId = GetUserId();
            var success = await _savedReportService.SaveReportAsync(id, userId);
            if (!success)
            {
                return BadRequest(BaseResponse<object>.FailureResult("Report is already saved or does not exist"));
            }

            return Ok(BaseResponse<object>.SuccessResult(new { reportId = id }, "Report saved successfully"));
        }

        /// <summary>
        /// Remove a saved report.
        /// </summary>
        [HttpDelete("{id}/save")]
        public async Task<IActionResult> UnsaveReport(int id)
        {
            var userId = GetUserId();
            var success = await _savedReportService.UnsaveReportAsync(id, userId);
            if (!success)
            {
                return NotFound(BaseResponse<object>.FailureResult("Saved report not found"));
            }

            return NoContent();
        }
    }
}
