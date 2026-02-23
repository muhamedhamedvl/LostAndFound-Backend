using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Application.DTOs.Report
{
    public class UpdateReportStatusDto
    {
        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = string.Empty;
    }
}
