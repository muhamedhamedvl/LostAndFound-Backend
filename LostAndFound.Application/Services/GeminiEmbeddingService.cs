using System.Text;
using System.Text.Json;
using LostAndFound.Application.Common.Exceptions;
using LostAndFound.Application.Interfaces;
using LostAndFound.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LostAndFound.Application.Services
{
    public class GeminiEmbeddingService : IEmbeddingService
    {
        public const int ExpectedEmbeddingDimensions = 3072;

        private const string EndpointFormat =
            "https://generativelanguage.googleapis.com/v1beta/models/{0}:embedContent?key={1}";

        private static readonly JsonSerializerOptions JsonWrite = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiEmbeddingService> _logger;

        public GeminiEmbeddingService(
            HttpClient httpClient,
            IOptions<GeminiOptions> options,
            ILogger<GeminiEmbeddingService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public Task<float[]> EmbedTextAsync(string text, CancellationToken cancellationToken = default)
            => GenerateEmbeddingAsync(text, cancellationToken);

        public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct)
        {
            if (text == null)
                throw new ArgumentException("Text is required.", nameof(text));

            text = text.Trim();
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text is required.", nameof(text));

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("Gemini:ApiKey is not configured.");

            var model = string.IsNullOrWhiteSpace(_options.Model) ? "text-embedding-004" : _options.Model.Trim();
            var url = string.Format(
                EndpointFormat,
                Uri.EscapeDataString(model),
                Uri.EscapeDataString(_options.ApiKey.Trim()));

            var payload = new
            {
                content = new
                {
                    parts = new[]
                    {
                        new { text }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload, JsonWrite);

            _logger.LogInformation("📤 GEMINI REQUEST JSON: {json}", json);

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("🔥 GEMINI RAW RESPONSE: {body}", responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini request failed. Status: {StatusCode}. Body: {Body}", (int)response.StatusCode, responseBody);
                throw new EmbeddingProviderApiException(
                    $"Gemini API returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseBody}",
                    (int)response.StatusCode);
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(responseBody);
            }
            catch (JsonException ex)
            {
                throw new EmbeddingProviderApiException("Gemini response is not valid JSON.", null, ex);
            }

            using (doc)
            {
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object ||
                    !root.TryGetProperty("embedding", out var embeddingEl) ||
                    embeddingEl.ValueKind != JsonValueKind.Object ||
                    !embeddingEl.TryGetProperty("values", out var valuesEl) ||
                    valuesEl.ValueKind != JsonValueKind.Array)
                {
                    throw new EmbeddingProviderApiException("Gemini response missing embedding.values.");
                }

                var length = valuesEl.GetArrayLength();
                if (length == 0)
                    throw new EmbeddingProviderApiException("Gemini embedding is empty.");

                if (length != ExpectedEmbeddingDimensions)
                    throw new EmbeddingDimensionException(length, ExpectedEmbeddingDimensions);

                var result = new float[ExpectedEmbeddingDimensions];
                var i = 0;
                foreach (var v in valuesEl.EnumerateArray())
                {
                    if (v.ValueKind != JsonValueKind.Number)
                        throw new EmbeddingProviderApiException("Gemini embedding contains non-numeric values.");

                    result[i++] = v.GetSingle();
                }

                return result;
            }
        }
    }
}

