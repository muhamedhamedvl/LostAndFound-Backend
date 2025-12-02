using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostAndFound.Domain.Entities
{
    public class ChatMessage : BaseEntity
    {
        public int ChatSessionId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }

        public string Text { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        public ChatSession? ChatSession { get; set; }
        public User? Sender { get; set; }
        public User? Receiver { get; set; }
    }
}
