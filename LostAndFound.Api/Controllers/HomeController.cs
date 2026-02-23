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
    [EnableRateLimiting("api")]
    public class HomeController : ControllerBase
    {
        private readonly IHomeService _homeService;

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        /// <summary>
        /// Get dashboard data: recent reports, stats, and categories count.
        /// When authenticated, includes the current user's reports count.
        /// </summary>
        [HttpGet("dashboard")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                int? userId = null;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var uid) && uid > 0)
                {
                    userId = uid;
                }

                var dashboard = await _homeService.GetDashboardAsync(userId);

                return Ok(BaseResponse<HomeDashboardDto>.SuccessResult(dashboard, "Dashboard retrieved successfully"));
            }
            catch (Exception)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult("An error occurred while retrieving the dashboard"));
            }
        }
    }
}
