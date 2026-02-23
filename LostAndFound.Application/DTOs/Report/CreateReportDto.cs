using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Application.DTOs.Report
{
    public class CreateReportDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type is required")]
        public string Type { get; set; } = string.Empty;

        public string? LocationName { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double? Longitude { get; set; }

        [Required(ErrorMessage = "SubCategoryId is required")]
        public int SubCategoryId { get; set; }

        public DateTime? DateReported { get; set; }

        public List<IFormFile>? Images { get; set; }
    }
}
