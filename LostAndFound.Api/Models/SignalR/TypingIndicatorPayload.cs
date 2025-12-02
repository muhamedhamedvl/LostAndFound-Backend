using System;

namespace LostAndFound.Api.Models.SignalR
{
    public class TypingIndicatorPayload
    {
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public int TargetUserId { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}

