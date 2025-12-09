namespace LostAndFound.Application.DTOs.Social
{
    /// <summary>
    /// DTO for notification information
    /// </summary>
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // "like", "comment", "message", "match"
        public string Content { get; set; } = string.Empty;
        public int? PostId { get; set; }
        public int? ActorId { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
