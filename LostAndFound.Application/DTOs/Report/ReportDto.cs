using LostAndFound.Domain.Enums;
using System;
using System.Collections.Generic;

namespace LostAndFound.Application.DTOs.Report
{
    public class ReportDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        /// <summary>Moderation lifecycle: Pending, Approved, Rejected, Matched, Closed, Archived, Flagged.</summary>
        public string LifecycleStatus { get; set; } = string.Empty;
        public string? LocationName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? MatchPercentage { get; set; }
        public int SubCategoryId { get; set; }
        public string? SubCategoryName { get; set; }
        public string? CategoryName { get; set; }
        public int CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public string? CreatedByProfilePictureUrl { get; set; }
        public DateTime? DateReported { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<ReportImageDto> Images { get; set; } = new();
    }
}
