using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Application.DTOs.Ai
{
    public class AiAddTextRequestDto
    {
        [Required]
        public string PostId { get; set; } = string.Empty;

        [Required]
        public string Text { get; set; } = string.Empty;
    }
}
