namespace LostAndFound.Application.DTOs.Social
{
    /// <summary>
    /// DTO for comment information
    /// </summary>
    public class CommentDto
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating a comment
    /// </summary>
    public class CreateCommentDto
    {
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating a comment
    /// </summary>
    public class UpdateCommentDto
    {
        public string Content { get; set; } = string.Empty;
    }
}
