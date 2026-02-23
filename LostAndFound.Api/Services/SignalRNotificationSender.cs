using LostAndFound.Api.Hubs;
using LostAndFound.Application.DTOs.Notification;
using LostAndFound.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LostAndFound.Api.Services
{
    public class SignalRNotificationSender : IRealtimeNotificationSender
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotificationSender(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task SendAsync(NotificationDto notification, CancellationToken ct = default)
        {
            return _hubContext.Clients
                .Group($"User_{notification.UserId}")
                .SendAsync("ReceiveNotification", notification, ct);
        }
    }
}
