using System.Linq;
using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Notification;
using LostAndFound.Application.Interfaces;
using LostAndFound.Api.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace LostAndFound.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("api")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IDeviceTokenService _deviceTokenService;
        private readonly FirebaseOptions? _firebaseOptions;

        public NotificationsController(
            INotificationService notificationService,
            IDeviceTokenService deviceTokenService,
            IOptions<FirebaseOptions>? firebaseOptions = null)
        {
            _notificationService = notificationService;
            _deviceTokenService = deviceTokenService;
            _firebaseOptions = firebaseOptions?.Value;
        }

        /// <summary>
        /// Get VAPID public key for web push (frontend device registration).
        /// Safe to expose; do not expose the private key.
        /// </summary>
        [HttpGet("vapid-public-key")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Get VAPID public key", Description = "Returns the VAPID public key for web push subscription. Use this in the frontend when registering for push. No auth required.")]
        public IActionResult GetVapidPublicKey()
        {
            if (string.IsNullOrWhiteSpace(_firebaseOptions?.VapidKey))
                return NotFound(BaseResponse<object>.FailureResult("VAPID key not configured"));
            return Ok(BaseResponse<object>.SuccessResult(new { vapidPublicKey = _firebaseOptions.VapidKey }, "VAPID public key"));
        }

        /// <summary>
        /// Get user's notifications.
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20)</param>
        /// <param name="type">Filter by read status: all (default), unread, read</param>
        /// <param name="category">Filter by notification category: all (default), general, matches</param>
        [HttpGet]
        [SwaggerOperation(Summary = "Get notifications", Description = "Retrieves notifications for the authenticated user with optional filters. Type filter: all/unread/read. Category filter: all/general/matches. Requires authentication.")]
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? type = null, [FromQuery] string? category = null)
        {
            try
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) || userId <= 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                var (notifications, totalCount) = await _notificationService.GetUserNotificationsAsync(userId, type, category, page, pageSize);

                return Ok(BaseResponse<object>.SuccessResult(new
                {
                    notifications,
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }, "Notifications retrieved successfully"));
            }
            catch (Exception)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult("An error occurred while retrieving notifications"));
            }
        }

        /// <summary>
        /// Get unread notification count
        /// </summary>
        [HttpGet("unread")]
        [SwaggerOperation(Summary = "Get unread count", Description = "Retrieves the count of unread notifications. Requires authentication.")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) || userId <= 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                var count = await _notificationService.GetUnreadCountAsync(userId);

                return Ok(BaseResponse<object>.SuccessResult(new { unreadCount = count }, "Unread count retrieved successfully"));
            }
            catch (Exception)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult("An error occurred while retrieving unread count"));
            }
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        [HttpPut("{id}/read")]
        [SwaggerOperation(Summary = "Mark as read", Description = "Marks a notification as read. Requires authentication.")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) || userId <= 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                await _notificationService.MarkAsReadAsync(id, userId);

                return Ok(BaseResponse<object>.SuccessResult(null!, "Notification marked as read"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(BaseResponse<object>.FailureResult("Notification not found"));
            }
            catch (Exception)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult("An error occurred while marking notification as read"));
            }
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        [HttpPost("mark-all-read")]
        [SwaggerOperation(Summary = "Mark all as read", Description = "Marks all notifications as read for the authenticated user. Requires authentication.")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) || userId <= 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                var updatedCount = await _notificationService.MarkAllAsReadAsync(userId);

                return Ok(BaseResponse<object>.SuccessResult(new { updatedCount }, "All notifications marked as read"));
            }
            catch (Exception)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult("An error occurred while marking all notifications as read"));
            }
        }

        /// <summary>
        /// Delete a notification
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete notification", Description = "Deletes a notification. Requires authentication.")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            try
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) || userId <= 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                await _notificationService.DeleteNotificationAsync(id, userId);

                return Ok(BaseResponse<object>.SuccessResult(null!, "Notification deleted successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(BaseResponse<object>.FailureResult("Notification not found"));
            }
            catch (Exception)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult("An error occurred while deleting the notification"));
            }
        }

        /// <summary>
        /// Register device token for push notifications (FCM/APNs).
        /// </summary>
        [HttpPost("register-device")]
        [SwaggerOperation(Summary = "Register device token", Description = "Registers or updates the device token for push notifications. Requires authentication.")]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenDto dto)
        {
            try
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) || userId <= 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                if (!ModelState.IsValid)
                    return BadRequest(BaseResponse<object>.FailureResult("Validation failed", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

                await _deviceTokenService.RegisterTokenAsync(userId, dto.Token, dto.Platform);

                return Ok(BaseResponse<object>.SuccessResult(null!, "Device token registered successfully"));
            }
            catch (Exception)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult("An error occurred while registering device token"));
            }
        }
    }
}
