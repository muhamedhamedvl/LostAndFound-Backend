namespace LostAndFound.Application.DTOs.Social
{
    /// <summary>
    /// DTO for like information
    /// </summary>
    public class LikeDto
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
