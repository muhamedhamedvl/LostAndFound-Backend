using System;

namespace LostAndFound.Application.DTOs.Report
{
    public class ReportMatchDto
    {
        public int Id { get; set; }
        public int ReportId { get; set; }
        public int MatchedReportId { get; set; }
        public string? MatchedReportTitle { get; set; }
        public double SimilarityScore { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
