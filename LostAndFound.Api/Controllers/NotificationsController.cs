using AutoMapper;
using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Social;
using LostAndFound.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace LostAndFound.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get user's notifications
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Get notifications", Description = "Retrieves all notifications for the authenticated user. Requires authentication.")]
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                var notifications = await _unitOfWork.Notifications.GetQueryable()
                    .Include(n => n.Actor)
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        Type = n.Type,
                        Content = n.Content,
                        PostId = n.PostId,
                        ActorId = n.ActorId,
                        ActorName = n.Actor != null ? n.Actor.FullName : "",
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt
                    })
                    .ToListAsync();

                var totalCount = await _unitOfWork.Notifications.GetQueryable()
                    .Where(n => n.UserId == userId)
                    .CountAsync();

                return Ok(BaseResponse<object>.SuccessResult(new
                {
                    notifications,
                    totalCount,
                    page,
                    pageSize
                }, "Notifications retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<List<NotificationDto>>.FailureResult($"Error retrieving notifications: {ex.Message}"));
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
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                var count = await _unitOfWork.Notifications.GetQueryable()
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .CountAsync();

                return Ok(BaseResponse<object>.SuccessResult(new { unreadCount = count }, "Unread count retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error retrieving unread count: {ex.Message}"));
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
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
                if (notification == null)
                    return NotFound(BaseResponse<object>.FailureResult("Notification not found"));

                if (notification.UserId != userId)
                    return Forbid();

                notification.IsRead = true;
                notification.UpdatedAt = DateTime.UtcNow;
                
                await _unitOfWork.Notifications.UpdateAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                return Ok(BaseResponse<object>.SuccessResult(null, "Notification marked as read"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error marking notification as read: {ex.Message}"));
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
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                var notifications = await _unitOfWork.Notifications.GetQueryable()
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.UpdatedAt = DateTime.UtcNow;
                }

                await _unitOfWork.SaveChangesAsync();

                return Ok(BaseResponse<object>.SuccessResult(new { updatedCount = notifications.Count }, "All notifications marked as read"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error marking all notifications as read: {ex.Message}"));
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
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
                if (notification == null)
                    return NotFound(BaseResponse<object>.FailureResult("Notification not found"));

                if (notification.UserId != userId)
                    return Forbid();

                await _unitOfWork.Notifications.DeleteAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                return Ok(BaseResponse<object>.SuccessResult(null, "Notification deleted successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error deleting notification: {ex.Message}"));
            }
        }
    }
}
