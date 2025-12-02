namespace LostAndFound.Application.DTOs.Item
{
    public class CreateCommentDto
    {
        public int PostId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}

