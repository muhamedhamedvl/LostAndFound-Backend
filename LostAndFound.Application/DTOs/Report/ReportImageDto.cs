namespace LostAndFound.Application.DTOs.Report
{
    public class ReportImageDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int ReportId { get; set; }
    }
}
