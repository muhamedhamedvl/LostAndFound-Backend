using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Application.DTOs.Report
{
    public class UpdateReportDto
    {
        [StringLength(200, MinimumLength = 3)]
        public string? Title { get; set; }

        [StringLength(2000, MinimumLength = 10)]
        public string? Description { get; set; }

        public string? Type { get; set; }

        public string? LocationName { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double? Longitude { get; set; }

        public int? SubCategoryId { get; set; }

        public DateTime? DateReported { get; set; }

        public List<int>? ImageIdsToRemove { get; set; }
        public List<IFormFile>? NewImages { get; set; }
    }
}
