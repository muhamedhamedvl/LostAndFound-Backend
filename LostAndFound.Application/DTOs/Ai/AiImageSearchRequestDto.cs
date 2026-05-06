using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LostAndFound.Application.DTOs.Ai
{
    public class AiImageSearchRequestDto
    {
        [Required]
        public IFormFile Image { get; set; } = default!;

        [Range(1, 100)]
        public int K { get; set; } = 5;
    }
}
