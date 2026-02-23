using System;

namespace LostAndFound.Domain.Entities
{
    /// <summary>
    /// Stores a match result between two reports.
    /// </summary>
    public class ReportMatch : BaseEntity
    {
        public int ReportId { get; set; }
        public Report Report { get; set; } = null!;

        public int MatchedReportId { get; set; }
        public Report MatchedReport { get; set; } = null!;

        public double SimilarityScore { get; set; }
    }
}
