using LostAndFound.Application.Common;
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
    public class MatchingController : ControllerBase
    {
        private readonly IMatchingService _matchingService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MatchingController> _logger;

        public MatchingController(IMatchingService matchingService, IUnitOfWork unitOfWork, ILogger<MatchingController> logger)
        {
            _matchingService = matchingService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Run matching algorithm for a specific report.
        /// Only the report owner or an Admin can run matching.
        /// </summary>
        [HttpPost("run/{reportId}")]
        public async Task<IActionResult> RunMatching(int reportId)
        {
            try
            {
                // Ownership check: only report owner or Admin
                var authResult = await AuthorizeReportOwnerOrAdmin(reportId);
                if (authResult != null) return authResult;

                var results = await _matchingService.RunMatchingAsync(reportId);
                var payload = new
                {
                    reportId,
                    matchesFound = results.Count,
                    matches = results
                };
                return Ok(BaseResponse<object>.SuccessResult(payload, "Matching completed successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(BaseResponse<object>.FailureResult("Report not found."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running matching for report {ReportId}", reportId);
                return StatusCode(500, BaseResponse<object>.FailureResult("An unexpected error occurred."));
            }
        }

        /// <summary>
        /// Get existing matches for a report.
        /// Only the report owner or an Admin can view matches.
        /// </summary>
        [HttpGet("{reportId}")]
        public async Task<IActionResult> GetMatches(int reportId)
        {
            try
            {
                // Ownership check: only report owner or Admin
                var authResult = await AuthorizeReportOwnerOrAdmin(reportId);
                if (authResult != null) return authResult;

                var matches = await _matchingService.GetMatchesAsync(reportId);
                var payload = new
                {
                    reportId,
                    totalMatches = matches.Count,
                    matches
                };
                return Ok(BaseResponse<object>.SuccessResult(payload, "Matches retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving matches for report {ReportId}", reportId);
                return StatusCode(500, BaseResponse<object>.FailureResult("An unexpected error occurred."));
            }
        }

        /// <summary>
        /// Returns null if authorized, or an IActionResult (Unauthorized/Forbid/NotFound) if not.
        /// </summary>
        private async Task<IActionResult?> AuthorizeReportOwnerOrAdmin(int reportId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));
            }

            var report = await _unitOfWork.Reports.GetByIdAsync(reportId);
            if (report == null)
            {
                return NotFound(BaseResponse<object>.FailureResult("Report not found."));
            }

            var isAdmin = User.IsInRole("Admin");
            if (report.CreatedById != currentUserId && !isAdmin)
            {
                return StatusCode(403, BaseResponse<object>.FailureResult("You can only access matches for your own reports."));
            }

            return null; // Authorized
        }
    }
}
