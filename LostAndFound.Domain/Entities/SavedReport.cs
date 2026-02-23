using System;

namespace LostAndFound.Domain.Entities
{
    public class SavedReport : BaseEntity
    {
        public int ReportId { get; set; }
        public Report Report { get; set; } = null!;

        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;
    }
}

