namespace LostAndFound.Application.DTOs.Report
{
    public class NearbyReportDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string? ImageUrl { get; set; }
    }
}
