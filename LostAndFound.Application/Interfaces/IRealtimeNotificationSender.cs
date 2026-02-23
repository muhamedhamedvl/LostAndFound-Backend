using LostAndFound.Application.DTOs.Notification;

namespace LostAndFound.Application.Interfaces
{
    public interface IRealtimeNotificationSender
    {
        Task SendAsync(NotificationDto notification, CancellationToken ct = default);
    }
}
