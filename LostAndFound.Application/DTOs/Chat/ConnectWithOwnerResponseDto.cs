namespace LostAndFound.Application.DTOs.Chat
{
    public class ConnectWithOwnerResponseDto
    {
        public int ConversationId { get; set; }
        public int PostId { get; set; }
        public int SenderUserId { get; set; }
        public int ReceiverUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
