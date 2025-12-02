using LostAndFound.Application.DTOs.Auth;

namespace LostAndFound.Application.DTOs.Post
{
    public class LikeDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public UserDto? User { get; set; }
        public int PostId { get; set; }
        public string ReactionType { get; set; } = "Like";
        public DateTime CreatedAt { get; set; }
    }

    public class CreateLikeDto
    {
        public int PostId { get; set; }
        public string ReactionType { get; set; } = "Like"; // "Like", "Love", "Helpful", "Sad"
    }
}

