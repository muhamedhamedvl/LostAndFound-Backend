using LostAndFound.Api.Hubs;
using LostAndFound.Api.Models.SignalR;
using LostAndFound.Api.Services.Interfaces;
using LostAndFound.Application.DTOs.Chat;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LostAndFound.Api.Services
{
    public class ChatHubService : IChatHubService
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<ChatHubService> _logger;

        public ChatHubService(IHubContext<ChatHub> hubContext, ILogger<ChatHubService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public Task NotifyMessageSentAsync(ChatMessageDto message)
        {
            _logger.LogDebug("Dispatching ReceiveMessage for session {SessionId} from {SenderId} to {ReceiverId}",
                message.ChatSessionId, message.SenderId, message.ReceiverId);

            return Task.WhenAll(
                _hubContext.Clients.Group(SessionGroup(message.ChatSessionId))
                    .SendAsync("ReceiveMessage", message),
                _hubContext.Clients.Group(UserGroup(message.ReceiverId))
                    .SendAsync("ReceiveMessage", message),
                _hubContext.Clients.Group(UserGroup(message.SenderId))
                    .SendAsync("ReceiveMessage", message)
            );
        }

        public Task NotifyUserTypingAsync(TypingIndicatorPayload payload)
        {
            _logger.LogDebug("Dispatching typing indicator for session {SessionId} user {UserId}",
                payload.SessionId, payload.UserId);

            var tasks = new List<Task>
            {
                _hubContext.Clients.Group(SessionGroup(payload.SessionId))
                    .SendAsync("UserTyping", payload),
                _hubContext.Clients.Group(UserGroup(payload.UserId))
                    .SendAsync("UserTyping", payload)
            };

            if (payload.TargetUserId > 0)
            {
                tasks.Add(_hubContext.Clients.Group(UserGroup(payload.TargetUserId))
                    .SendAsync("UserTyping", payload));
            }

            return Task.WhenAll(tasks);
        }

        public Task NotifyMessageReadAsync(ChatMessageDto message)
        {
            _logger.LogDebug("Dispatching MessageRead for message {MessageId}", message.Id);

            return Task.WhenAll(
                _hubContext.Clients.Group(SessionGroup(message.ChatSessionId))
                    .SendAsync("MessageRead", message),
                _hubContext.Clients.Group(UserGroup(message.SenderId))
                    .SendAsync("MessageRead", message),
                _hubContext.Clients.Group(UserGroup(message.ReceiverId))
                    .SendAsync("MessageRead", message)
            );
        }

        public Task NotifySessionCreatedAsync(SessionCreatedPayload payload)
        {
            _logger.LogInformation("Dispatching SessionCreated for session {SessionId}", payload.SessionId);
            return Task.WhenAll(
                _hubContext.Clients.Group(UserGroup(payload.InitiatorUserId))
                    .SendAsync("SessionCreated", payload),
                _hubContext.Clients.Group(UserGroup(payload.TargetUserId))
                    .SendAsync("SessionCreated", payload)
            );
        }

        public Task NotifyPresenceChangedAsync(PresenceStatusPayload payload)
        {
            _logger.LogDebug("Dispatching presence change for user {UserId} online={IsOnline}", payload.UserId, payload.IsOnline);
            return _hubContext.Clients.All.SendAsync(payload.IsOnline ? "UserOnline" : "UserOffline", payload);
        }

        private static string SessionGroup(int sessionId) => $"ChatSession_{sessionId}";
        private static string UserGroup(int userId) => $"User_{userId}";
    }
}

