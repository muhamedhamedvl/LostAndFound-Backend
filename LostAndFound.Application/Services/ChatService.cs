using AutoMapper;
using System.Linq;
using LostAndFound.Application.DTOs.Auth;
using LostAndFound.Application.DTOs.Chat;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Application.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ChatService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ChatSessionSummaryDto>> GetUserSessionsAsync(int userId)
        {
            var sessionsQuery = _unitOfWork.ChatSessions
                .GetQueryable()
                .Where(s => s.User1Id == userId || s.User2Id == userId)
                .Include(s => s.User1)
                .Include(s => s.User2)
                .AsNoTracking();

            var sessions = await sessionsQuery
                .OrderByDescending(s => s.LastMessageTime ?? s.CreatedAt)
                .ToListAsync();

            if (!sessions.Any())
            {
                return Enumerable.Empty<ChatSessionSummaryDto>();
            }

            var sessionIds = sessions.Select(s => s.Id).ToList();

            var lastMessageIds = await _unitOfWork.ChatMessages
                .GetQueryable()
                .Where(m => sessionIds.Contains(m.ChatSessionId))
                .OrderByDescending(m => m.SentAt)
                .ThenByDescending(m => m.Id)
                .GroupBy(m => m.ChatSessionId)
                .Select(g => g.Select(m => m.Id).First())
                .ToListAsync();

            var lastMessageLookup = new Dictionary<int, ChatMessage>();

            if (lastMessageIds.Any())
            {
                var lastMessages = await _unitOfWork.ChatMessages
                    .GetQueryable()
                    .Where(m => lastMessageIds.Contains(m.Id))
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .AsNoTracking()
                    .ToListAsync();

                lastMessageLookup = lastMessages.ToDictionary(m => m.ChatSessionId, m => m);
            }

            var unreadSessionIds = await _unitOfWork.ChatMessages
                .GetQueryable()
                .Where(m => sessionIds.Contains(m.ChatSessionId) && m.ReceiverId == userId && !m.IsRead)
                .Select(m => m.ChatSessionId)
                .Distinct()
                .ToListAsync();

            var unreadLookup = unreadSessionIds.ToHashSet();

            var summaries = sessions
                .Select(session => new ChatSessionSummaryDto
                {
                    Id = session.Id,
                    CreatedAt = session.CreatedAt,
                    LastMessageTime = session.LastMessageTime ?? session.CreatedAt,
                    OtherUser = MapUserForSummary(session, userId),
                    LastMessage = lastMessageLookup.TryGetValue(session.Id, out var message)
                        ? _mapper.Map<ChatMessageDto>(message)
                        : null,
                    HasUnreadMessages = unreadLookup.Contains(session.Id)
                })
                .OrderByDescending(s => s.LastMessageTime)
                .ToList();

            return summaries;
        }

        public async Task<ChatSessionDetailsDto> GetSessionDetailsAsync(int sessionId, int userId)
        {
            var session = await GetSessionAndValidateAsync(sessionId, userId, trackChanges: false, includeUsers: true);
            return _mapper.Map<ChatSessionDetailsDto>(session);
        }

        public async Task<ChatSessionDetailsDto> OpenOrCreateSessionAsync(int currentUserId, int otherUserId)
        {
            if (currentUserId == otherUserId)
            {
                throw new ArgumentException("You cannot start a chat session with yourself.");
            }

            var otherUser = await _unitOfWork.Users.GetByIdAsync(otherUserId);
            if (otherUser == null)
            {
                throw new KeyNotFoundException("The user you are trying to reach does not exist.");
            }

            var existingSession = await _unitOfWork.ChatSessions.FirstOrDefaultAsync(s =>
                (s.User1Id == currentUserId && s.User2Id == otherUserId) ||
                (s.User1Id == otherUserId && s.User2Id == currentUserId));

            ChatSession session;
            if (existingSession != null)
            {
                session = existingSession;
            }
            else
            {
                session = new ChatSession
                {
                    User1Id = currentUserId,
                    User2Id = otherUserId,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageTime = DateTime.UtcNow
                };

                await _unitOfWork.ChatSessions.AddAsync(session);
                await _unitOfWork.SaveChangesAsync();
            }

            var hydratedSession = await _unitOfWork.ChatSessions
                .GetQueryable()
                .Include(s => s.User1)
                .Include(s => s.User2)
                .AsNoTracking()
                .FirstAsync(s => s.Id == session.Id);

            return _mapper.Map<ChatSessionDetailsDto>(hydratedSession);
        }

        public async Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(int sessionId, int userId)
        {
            await GetSessionAndValidateAsync(sessionId, userId, trackChanges: false, includeUsers: false);

            var messages = await _unitOfWork.ChatMessages
                .GetQueryable()
                .Where(m => m.ChatSessionId == sessionId)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderBy(m => m.SentAt)
                .ThenBy(m => m.Id)
                .AsNoTracking()
                .ToListAsync();

            return _mapper.Map<IEnumerable<ChatMessageDto>>(messages);
        }

        public async Task<IEnumerable<ChatMessageDto>> SendMessageAsync(int sessionId, int senderId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Message text cannot be empty.");
            }

            var session = await GetSessionAndValidateAsync(sessionId, senderId, trackChanges: true, includeUsers: false);

            var receiverId = session.User1Id == senderId ? session.User2Id : session.User1Id;

            if (receiverId == 0)
            {
                throw new InvalidOperationException("Cannot determine the receiver for this session.");
            }

            var message = new ChatMessage
            {
                ChatSessionId = sessionId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Text = text.Trim(),
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _unitOfWork.ChatMessages.AddAsync(message);

            session.LastMessageTime = message.SentAt;
            session.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.ChatSessions.UpdateAsync(session);

            await _unitOfWork.SaveChangesAsync();

            return await GetMessagesAsync(sessionId, senderId);
        }

        public async Task<ChatMessageDto> MarkMessageAsReadAsync(int messageId, int userId)
        {
            var message = await _unitOfWork.ChatMessages
                .GetQueryable()
                .Include(m => m.ChatSession)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                throw new KeyNotFoundException("Message not found.");
            }

            if (message.ReceiverId != userId)
            {
                throw new UnauthorizedAccessException("You can only mark your own received messages as read.");
            }

            if (!message.IsRead)
            {
                message.IsRead = true;
                message.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.ChatMessages.UpdateAsync(message);
                await _unitOfWork.SaveChangesAsync();
            }

            return _mapper.Map<ChatMessageDto>(message);
        }

        private async Task<ChatSession> GetSessionAndValidateAsync(int sessionId, int userId, bool trackChanges, bool includeUsers)
        {
            var query = _unitOfWork.ChatSessions.GetQueryable();

            if (!trackChanges)
            {
                query = query.AsNoTracking();
            }

            if (includeUsers)
            {
                query = query
                    .Include(s => s.User1)
                    .Include(s => s.User2);
            }

            var session = await query.FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                throw new KeyNotFoundException("Chat session not found.");
            }

            if (session.User1Id != userId && session.User2Id != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to access this chat session.");
            }

            return session;
        }

        private UserDto? MapUserForSummary(ChatSession session, int currentUserId)
        {
            var otherUser = session.User1Id == currentUserId ? session.User2 : session.User1;
            if (otherUser == null)
            {
                return null;
            }

            return _mapper.Map<UserDto>(otherUser);
        }
    }
}

