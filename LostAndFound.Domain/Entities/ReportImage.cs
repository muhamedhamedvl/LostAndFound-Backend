namespace LostAndFound.Domain.Entities
{
    /// <summary>
    /// Represents an image attached to a report.
    /// </summary>
    public class ReportImage : BaseEntity
    {
        public string ImageUrl { get; set; } = string.Empty;

        public int ReportId { get; set; }
        public Report Report { get; set; } = null!;
    }
}
