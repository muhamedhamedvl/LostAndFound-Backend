using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Application.DTOs.Ai
{
    public class AiTextSearchRequestDto
    {
        [Required]
        public string Text { get; set; } = string.Empty;

        [Range(1, 100)]
        public int K { get; set; } = 5;
    }
}
