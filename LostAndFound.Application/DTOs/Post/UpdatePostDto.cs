using Microsoft.AspNetCore.Http;

namespace LostAndFound.Application.DTOs.Post
{
    public class UpdatePostDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public int SubCategoryId { get; set; }
        public string? Status { get; set; } // "Active", "Resolved", "Closed"
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Address { get; set; }
        public decimal? Reward { get; set; }
        public List<IFormFile>? NewPhotos { get; set; } // New photos to add (optional)
        public List<int>? PhotoIdsToRemove { get; set; } // Photo IDs to remove (optional)
    }
}

