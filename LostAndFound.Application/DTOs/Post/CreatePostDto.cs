using Microsoft.AspNetCore.Http;

namespace LostAndFound.Application.DTOs.Post
{
    public class CreatePostDto
    {
        public string Content { get; set; } = string.Empty;
        public int SubCategoryId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Address { get; set; }
        public List<IFormFile> Photos { get; set; } = new();
        public decimal? Reward { get; set; }
    }
}

