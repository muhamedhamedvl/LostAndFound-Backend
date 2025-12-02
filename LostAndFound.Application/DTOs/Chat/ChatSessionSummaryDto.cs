using LostAndFound.Application.DTOs.Auth;

namespace LostAndFound.Application.DTOs.Chat
{
    public class ChatSessionSummaryDto
    {
        public int Id { get; set; }
        public UserDto? OtherUser { get; set; }
        public ChatMessageDto? LastMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public bool HasUnreadMessages { get; set; }
    }
}

