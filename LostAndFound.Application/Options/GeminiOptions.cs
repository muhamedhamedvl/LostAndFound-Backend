namespace LostAndFound.Application.Options
{
    public class GeminiOptions
    {
        public const string SectionName = "Gemini";

        public string ApiKey { get; set; } = string.Empty;

        public string Model { get; set; } = "text-embedding-004";
    }
}

