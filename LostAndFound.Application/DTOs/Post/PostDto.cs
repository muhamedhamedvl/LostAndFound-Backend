using LostAndFound.Application.DTOs.Category;
using LostAndFound.Application.DTOs.Auth;
using LostAndFound.Application.DTOs.Item;

namespace LostAndFound.Application.DTOs.Post
{
    public class PostDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public int SubCategoryId { get; set; }
        public SubCategoryDto? SubCategory { get; set; }
        public int CategoryId { get; set; } // For convenience - the parent category
        public string CategoryName { get; set; } = string.Empty; // For convenience
        public string Status { get; set; } = "Active"; // "Active", "Resolved", "Closed"
        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedByUserId { get; set; }
        public int CreatorId { get; set; }
        public UserDto? Creator { get; set; }
        public int? OwnerId { get; set; }
        public UserDto? Owner { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<PhotoDto> Photos { get; set; } = new();
        public decimal? Reward { get; set; }
        
        // Social features
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public int SharesCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
    }
}
