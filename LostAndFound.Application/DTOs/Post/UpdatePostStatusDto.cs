namespace LostAndFound.Application.DTOs.Post
{
    public class UpdatePostStatusDto
    {
        public string Status { get; set; } = string.Empty; // "Active", "Resolved", "Closed"
    }
}

