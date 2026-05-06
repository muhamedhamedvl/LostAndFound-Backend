namespace LostAndFound.Application.DTOs.Ai
{
    public class AiResultDto
    {
        public double Score { get; set; }
        public string PostId { get; set; } = string.Empty;
        public string? PersonId { get; set; }
        public string? Text { get; set; }
    }
}

