using System;

namespace LostAndFound.Api.Models.SignalR
{
    public class PresenceStatusPayload
    {
        public int UserId { get; set; }
        public bool IsOnline { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}

