using LostAndFound.Api.Hubs;
using LostAndFound.Api.Services.Interfaces;
using LostAndFound.Application.DTOs.Notification;
using Microsoft.AspNetCore.SignalR;

namespace LostAndFound.Api.Services
{
    public class NotificationHubService : INotificationHubService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationHubService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task SendNotificationAsync(NotificationDto notification)
        {
            return _hubContext.Clients.Group($"User_{notification.UserId}").SendAsync("ReceiveNotification", notification);
        }
    }
}

