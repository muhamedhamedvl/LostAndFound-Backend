using LostAndFound.Application.DTOs.Auth;

namespace LostAndFound.Application.DTOs.Post
{
    public class ShareDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public UserDto? User { get; set; }
        public int PostId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateShareDto
    {
        public int PostId { get; set; }
    }
}

