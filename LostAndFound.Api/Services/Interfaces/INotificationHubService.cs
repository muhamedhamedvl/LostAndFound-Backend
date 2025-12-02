using LostAndFound.Application.DTOs.Notification;

namespace LostAndFound.Api.Services.Interfaces
{
    public interface INotificationHubService
    {
        Task SendNotificationAsync(NotificationDto notification);
    }
}

