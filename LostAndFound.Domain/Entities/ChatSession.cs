using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostAndFound.Domain.Entities
{
    public class ChatSession : BaseEntity
    {
        public int User1Id { get; set; }
        public int User2Id { get; set; }
        public DateTime? LastMessageTime { get; set; }

        public AppUser? User1 { get; set; }
        public AppUser? User2 { get; set; }
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
