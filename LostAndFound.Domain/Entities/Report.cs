using LostAndFound.Domain.Enums;
using System;
using System.Collections.Generic;

namespace LostAndFound.Domain.Entities
{
    /// <summary>
    /// Represents a lost or found report in the system.
    /// </summary>
    public class Report : BaseEntity
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public ReportType Type { get; set; }

        public ReportStatus Status { get; set; } = ReportStatus.Open;

        // New lifecycle status for moderation and state transitions, kept alongside legacy Status for backward compatibility.
        public ReportLifecycleStatus LifecycleStatus { get; set; } = ReportLifecycleStatus.Pending;

        public DateTime? DateReported { get; set; }

        public string? LocationName { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public double? MatchPercentage { get; set; }

        public int SubCategoryId { get; set; }
        public SubCategory SubCategory { get; set; } = null!;

        public int CreatedById { get; set; }
        public AppUser CreatedBy { get; set; } = null!;

        public ICollection<ReportImage> Images { get; set; } = new List<ReportImage>();
    }
}
