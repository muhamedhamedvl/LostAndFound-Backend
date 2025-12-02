using System;
using System.Linq;
using LostAndFound.Api.Models.SignalR;
using System.Security.Claims;
using LostAndFound.Application.DTOs.Chat;
using LostAndFound.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using LostAndFound.Api.Services.Interfaces;
namespace LostAndFound.Api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly IUserConnectionManager _connectionManager;
        private readonly IChatHubService _chatHubService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IChatService chatService,
            IUserConnectionManager connectionManager,
            IChatHubService chatHubService,
            ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _connectionManager = connectionManager;
            _chatHubService = chatHubService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            await RegisterCurrentConnectionAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserIdOrDefault();
            if (userId.HasValue)
            {
                var becameOffline = _connectionManager.RemoveConnection(userId.Value, Context.ConnectionId);
                if (becameOffline)
                {
                    await _chatHubService.NotifyPresenceChangedAsync(new PresenceStatusPayload
                    {
                        UserId = userId.Value,
                        IsOnline = false
                    });
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task RegisterUser(int userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != userId)
            {
                throw new HubException("You can only register your own user id.");
            }

            await RegisterCurrentConnectionAsync();
        }

        public async Task SendMessage(int sessionId, string content)
        {
            var userId = GetCurrentUserId();
            try
            {
                var messages = await _chatService.SendMessageAsync(sessionId, userId, content);
                var latestMessage = messages.LastOrDefault();
                if (latestMessage != null)
                {
                    await _chatHubService.NotifyMessageSentAsync(latestMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendMessage failed for session {SessionId} by user {UserId}", sessionId, userId);
                throw new HubException(ex.Message);
            }
        }

        public async Task Typing(int sessionId)
        {
            var userId = GetCurrentUserId();
            try
            {
                var session = await _chatService.GetSessionDetailsAsync(sessionId, userId);
                var targetUserId = session.User1Id == userId ? session.User2Id : session.User1Id;
                await _chatHubService.NotifyUserTypingAsync(new TypingIndicatorPayload
                {
                    SessionId = sessionId,
                    UserId = userId,
                    TargetUserId = targetUserId,
                    SentAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Typing failed for session {SessionId} user {UserId}", sessionId, userId);
                throw new HubException(ex.Message);
            }
        }

        public async Task MarkAsRead(int messageId)
        {
            var userId = GetCurrentUserId();
            try
            {
                var message = await _chatService.MarkMessageAsReadAsync(messageId, userId);
                await _chatHubService.NotifyMessageReadAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MarkAsRead failed for message {MessageId} user {UserId}", messageId, userId);
                throw new HubException(ex.Message);
            }
        }

        private async Task RegisterCurrentConnectionAsync()
        {
            var userId = GetCurrentUserId();
            var becameOnline = _connectionManager.AddConnection(userId, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

            if (becameOnline)
            {
                await _chatHubService.NotifyPresenceChangedAsync(new PresenceStatusPayload
                {
                    UserId = userId,
                    IsOnline = true
                });
            }
        }

        private int GetCurrentUserId()
        {
            var userId = Context.User?.FindFirst("nameid")?.Value
                         ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userId, out var parsed) || parsed <= 0)
            {
                throw new HubException("Invalid user context.");
            }

            return parsed;
        }

        private int? GetCurrentUserIdOrDefault()
        {
            var userId = Context.User?.FindFirst("nameid")?.Value
                         ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userId, out var parsed) && parsed > 0)
            {
                return parsed;
            }

            return null;
        }

        private static string UserGroup(int userId) => $"User_{userId}";
    }
}

