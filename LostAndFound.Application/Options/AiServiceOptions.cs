namespace LostAndFound.Application.Options
{
    public class AiServiceOptions
    {
        public const string SectionName = "AiService";

        public string BaseUrl { get; set; } = "https://mhmdwaleed309--lost-found-ai-fastapi-app.modal.run";

        public int TimeoutSeconds { get; set; } = 60;

        public string GetNormalizedBaseUrl()
        {
            var rawBaseUrl = string.IsNullOrWhiteSpace(BaseUrl)
                ? "https://mhmdwaleed309--lost-found-ai-fastapi-app.modal.run"
                : BaseUrl.Trim();

            if (!Uri.TryCreate(rawBaseUrl, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException($"AiService:BaseUrl '{BaseUrl}' is not a valid absolute URL.");
            }

            var builder = new UriBuilder(uri)
            {
                Fragment = string.Empty
            };

            var path = builder.Path.TrimEnd('/');
            if (path.EndsWith("/docs", StringComparison.OrdinalIgnoreCase))
            {
                path = path[..^"/docs".Length];
            }

            builder.Path = string.IsNullOrEmpty(path) ? "/" : path;

            return builder.Uri.AbsoluteUri.TrimEnd('/');
        }
    }
}
