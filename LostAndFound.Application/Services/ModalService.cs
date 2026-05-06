using System.Text;
using System.Text.Json;
using LostAndFound.Application.Common.Exceptions;
using LostAndFound.Application.Interfaces;
using LostAndFound.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LostAndFound.Application.Services
{
    public class ModalService : IModalService
    {
        private readonly HttpClient _httpClient;
        private readonly ModalOptions _options;
        private readonly ILogger<ModalService> _logger;

        public ModalService(
            HttpClient httpClient,
            IOptions<ModalOptions> options,
            ILogger<ModalService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;

            var baseUrl = (_options.BaseUrl ?? string.Empty).TrimEnd('/');
            if (!string.IsNullOrEmpty(baseUrl))
                _httpClient.BaseAddress = new Uri(baseUrl + "/");

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", _options.ApiKey);
        }

        public async Task<JsonElement> SearchByEmbeddingAsync(
            float[] embedding,
            string? indexNameOverride = null,
            CancellationToken cancellationToken = default)
        {
            if (embedding == null || embedding.Length == 0)
                throw new ArgumentException("Embedding is required.", nameof(embedding));

            const int expectedDimensions = 3072;
            if (embedding.Length != expectedDimensions)
                throw new EmbeddingDimensionException(embedding.Length, expectedDimensions);

            var indexName = !string.IsNullOrWhiteSpace(indexNameOverride)
                ? indexNameOverride.Trim()
                : _options.DefaultIndexName?.Trim();

            if (string.IsNullOrWhiteSpace(indexName))
                throw new InvalidOperationException(
                    "Modal index name is required. Set Modal:DefaultIndexName or pass indexName in the request.");

            var searchPath = string.IsNullOrWhiteSpace(_options.SearchPath)
                ? "/search-vector"
                : (_options.SearchPath.StartsWith('/') ? _options.SearchPath : "/" + _options.SearchPath);

            // ── First attempt ────────────────────────────────────────────────────
            _logger.LogInformation("Calling Modal {Path} for index '{Index}'.", searchPath, indexName);
            var (firstBody, firstStatus, firstSuccess) =
                await SendSearchRequestAsync(embedding, indexName, searchPath, cancellationToken);

            if (firstSuccess)
            {
                using var doc = JsonDocument.Parse(firstBody);
                return doc.RootElement.Clone();
            }

            // ── Index missing → initialise once, then retry ───────────────────────
            if (IsIndexNotFound(firstBody, firstStatus))
            {
                _logger.LogWarning(
                    "Modal returned 'Requested entity was not found' for index '{Index}'. " +
                    "Attempting one-time index initialisation via /add-vector.", indexName);

                await InitIndexAsync(embedding, indexName, cancellationToken);

                _logger.LogInformation(
                    "Retrying Modal {Path} for index '{Index}' after init.", searchPath, indexName);

                var (retryBody, retryStatus, retrySuccess) =
                    await SendSearchRequestAsync(embedding, indexName, searchPath, cancellationToken);

                if (retrySuccess)
                {
                    using var retryDoc = JsonDocument.Parse(retryBody);
                    return retryDoc.RootElement.Clone();
                }

                _logger.LogWarning(
                    "Modal search failed after init: {Status} {Body}", retryStatus, Truncate(retryBody));
                throw new ModalApiException(
                    $"Modal API returned {retryStatus} after index initialisation.", retryStatus);
            }

            // ── Any other error ───────────────────────────────────────────────────
            _logger.LogWarning("Modal search failed: {Status} {Body}", firstStatus, Truncate(firstBody));
            throw new ModalApiException($"Modal API returned {firstStatus}.", firstStatus);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sends a /search-vector request.
        /// Returns (responseBody, httpStatusCode, isSuccess) — never throws on HTTP errors.
        /// </summary>
        private async Task<(string Body, int Status, bool Success)> SendSearchRequestAsync(
            float[] embedding, string indexName, string path, CancellationToken cancellationToken)
        {
            // Modal contract: embedding + index_name + optional k — never user text.
            var payload = new
            {
                embedding,
                index_name = indexName,
                k = _options.K > 0 ? _options.K : 5
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Modal search request failed (network).");
                throw new ModalApiException("Failed to reach Modal API.", null, ex);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return (body, (int)response.StatusCode, response.IsSuccessStatusCode);
        }

        /// <summary>
        /// Calls /add-vector ONCE with a sentinel { init: true } metadata to create the index.
        /// Errors are logged but do NOT re-throw, so the retry search can still proceed.
        /// </summary>
        private async Task InitIndexAsync(float[] embedding, string indexName, CancellationToken cancellationToken)
        {
            var addPath = string.IsNullOrWhiteSpace(_options.AddVectorPath)
                ? "/add-vector"
                : (_options.AddVectorPath.StartsWith('/') ? _options.AddVectorPath : "/" + _options.AddVectorPath);

            var initPayload = new
            {
                embedding,
                metadata = new Dictionary<string, object> { ["init"] = true },
                index_name = indexName
            };

            using var initRequest = new HttpRequestMessage(HttpMethod.Post, addPath)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(initPayload), Encoding.UTF8, "application/json")
            };

            try
            {
                var initResponse = await _httpClient.SendAsync(initRequest, cancellationToken);
                var initBody = await initResponse.Content.ReadAsStringAsync(cancellationToken);

                if (initResponse.IsSuccessStatusCode)
                    _logger.LogInformation(
                        "Index '{Index}' initialised successfully via {Path}.", indexName, addPath);
                else
                    _logger.LogWarning(
                        "Index init call returned {Status}: {Body}",
                        (int)initResponse.StatusCode, Truncate(initBody));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Index init call to {Path} failed (network). Will still retry search.", addPath);
            }
        }

        /// <summary>
        /// Returns true when Modal signals that the requested index does not exist yet.
        /// Covers 400 / 404 / 500 status codes with the known error phrase.
        /// </summary>
        private static bool IsIndexNotFound(string body, int statusCode)
        {
            if (string.IsNullOrEmpty(body)) return false;

            return (statusCode is 400 or 404 or 500)
                   && body.Contains("Requested entity was not found", StringComparison.OrdinalIgnoreCase);
        }

        private static string Truncate(string s, int max = 500)
            => s.Length <= max ? s : s[..max] + "...";
    }
}
