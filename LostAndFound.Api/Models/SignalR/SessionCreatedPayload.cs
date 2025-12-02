using LostAndFound.Application.DTOs.Chat;
using System;

namespace LostAndFound.Api.Models.SignalR
{
    public class SessionCreatedPayload
    {
        public int SessionId { get; set; }
        public int InitiatorUserId { get; set; }
        public int TargetUserId { get; set; }
        public ChatSessionDetailsDto Session { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

