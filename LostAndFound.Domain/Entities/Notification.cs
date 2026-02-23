using System;

namespace LostAndFound.Domain.Entities
{
    /// <summary>
    /// Represents a notification sent to a user.
    /// </summary>
    public class Notification : BaseEntity
    {
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;

        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public int? ReportId { get; set; }
        public Report? Report { get; set; }

        public int? ActorId { get; set; }
        public AppUser? Actor { get; set; }

        public bool IsRead { get; set; } = false;
    }
}
