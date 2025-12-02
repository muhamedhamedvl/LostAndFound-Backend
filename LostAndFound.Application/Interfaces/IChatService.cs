using LostAndFound.Application.DTOs.Chat;

namespace LostAndFound.Application.Interfaces
{
    public interface IChatService
    {
        Task<IEnumerable<ChatSessionSummaryDto>> GetUserSessionsAsync(int userId);
        Task<ChatSessionDetailsDto> GetSessionDetailsAsync(int sessionId, int userId);
        Task<ChatSessionDetailsDto> OpenOrCreateSessionAsync(int currentUserId, int otherUserId);
        Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(int sessionId, int userId);
        Task<IEnumerable<ChatMessageDto>> SendMessageAsync(int sessionId, int senderId, string text);
        Task<ChatMessageDto> MarkMessageAsReadAsync(int messageId, int userId);
    }
}

