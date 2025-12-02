namespace LostAndFound.Application.DTOs.Item
{
    public class UpdateItemStatusDto
    {
        public string Status { get; set; } = string.Empty; // "Active", "Resolved", "Closed"
    }
}

