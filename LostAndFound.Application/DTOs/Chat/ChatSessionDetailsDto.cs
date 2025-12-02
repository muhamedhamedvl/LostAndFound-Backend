using LostAndFound.Application.DTOs.Auth;

namespace LostAndFound.Application.DTOs.Chat
{
    public class ChatSessionDetailsDto
    {
        public int Id { get; set; }
        public int User1Id { get; set; }
        public int User2Id { get; set; }
        public UserDto? User1 { get; set; }
        public UserDto? User2 { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageTime { get; set; }
    }
}

