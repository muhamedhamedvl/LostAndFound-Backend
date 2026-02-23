using LostAndFound.Application.Common;
using LostAndFound.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LostAndFound.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("api")]
    public class MatchingController : ControllerBase
    {
        private readonly IMatchingService _matchingService;

        public MatchingController(IMatchingService matchingService)
        {
            _matchingService = matchingService;
        }

        /// <summary>
        /// Run matching algorithm for a specific report.
        /// </summary>
        [HttpPost("run/{reportId}")]
        public async Task<IActionResult> RunMatching(int reportId)
        {
            try
            {
                var results = await _matchingService.RunMatchingAsync(reportId);
                var payload = new
                {
                    reportId,
                    matchesFound = results.Count,
                    matches = results
                };
                return Ok(BaseResponse<object>.SuccessResult(payload, "Matching completed successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"An error occurred: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get existing matches for a report.
        /// </summary>
        [HttpGet("{reportId}")]
        public async Task<IActionResult> GetMatches(int reportId)
        {
            try
            {
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
                return StatusCode(500, BaseResponse<object>.FailureResult($"An error occurred: {ex.Message}"));
            }
        }
    }
}
