namespace LostAndFound.Application.DTOs.Ai
{
    public class AiSearchRequestDto
    {
        /// <summary>Natural language query; embedded only — never sent to Modal.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Optional Modal index name override; defaults to configuration Modal:DefaultIndexName.</summary>
        public string? IndexName { get; set; }
    }
}
