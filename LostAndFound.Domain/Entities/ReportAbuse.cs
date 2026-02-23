using System;

namespace LostAndFound.Domain.Entities
{
    public class ReportAbuse : BaseEntity
    {
        public int ReportId { get; set; }
        public Report Report { get; set; } = null!;

        public int ReporterId { get; set; }
        public AppUser Reporter { get; set; } = null!;

        public string Reason { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}

