using LostAndFound.Application.DTOs.Category;
using LostAndFound.Application.DTOs.Location;
using LostAndFound.Application.DTOs.Auth;

namespace LostAndFound.Application.DTOs.Item
{
    public class ItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public CategoryDto? Category { get; set; }
        public string ReportType { get; set; } = string.Empty; // "Lost" or "Found"
        public string Status { get; set; } = "Active"; // "Active", "Resolved", "Closed"
        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedByUserId { get; set; }
        public int CreatorId { get; set; }
        public UserDto? Creator { get; set; }
        public int? LocationId { get; set; }
        public LocationDto? Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<PhotoDto> Photos { get; set; } = new();
        public List<CommentDto> Comments { get; set; } = new();
        public RewardDto? Reward { get; set; }
    }

    public class RewardDto
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public bool IsClaimed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

   
}