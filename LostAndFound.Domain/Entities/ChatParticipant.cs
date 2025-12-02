namespace LostAndFound.Domain.Entities
{
    public class ChatParticipant : BaseEntity
    {
        public int ChatSessionId { get; set; }
        public int UserId { get; set; }

        public ChatSession? ChatSession { get; set; }
        public User? User { get; set; }
    }
}

