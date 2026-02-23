using LostAndFound.Application.DTOs.Notification;
using LostAndFound.Domain.Entities;

namespace LostAndFound.Application.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationDto> CreateNotificationAsync(int userId, string type, string message, int? reportId = null, string? title = null);
        Task<(IEnumerable<NotificationDto> Notifications, int TotalCount)> GetUserNotificationsAsync(int userId, string? typeFilter = null, string? category = null, int page = 1, int pageSize = 20);
        Task<IEnumerable<NotificationDto>> GetUnreadNotificationsAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<NotificationDto?> GetNotificationByIdAsync(int id, int userId);
        Task<NotificationDto> MarkAsReadAsync(int id, int userId);
        Task<int> MarkAllAsReadAsync(int userId);
        Task DeleteNotificationAsync(int id, int userId);
        Task<NotificationDto?> NotifyReportMatchAsync(Report report, Report matchedReport, double score);
        Task<NotificationDto?> NotifyReportStatusChangeAsync(int reportId, string newStatus);
        Task<NotificationDto?> NotifyInterestedInReportAsync(int reportId, int interestedUserId);
        Task<NotificationDto?> NotifyNewMessageAsync(int recipientUserId, int senderUserId, string senderName, int chatSessionId);
        Task<NotificationDto?> NotifyLocationAlertAsync(int userId, int reportId, string reportTitle, double distanceKm);
    }
}
