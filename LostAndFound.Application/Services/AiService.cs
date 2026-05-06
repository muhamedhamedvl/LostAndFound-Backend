using System.Net;
using System.Text.Json;
using LostAndFound.Application.Common.Exceptions;
using LostAndFound.Application.DTOs.Ai;
using LostAndFound.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Application.Services
{
    public class AiService : IAiService
    {
        private const int MaxRetries = 2;

        private readonly HttpClient _httpClient;
        private readonly ILogger<AiService> _logger;

        public AiService(HttpClient httpClient, ILogger<AiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public Task<JsonElement> AddTextAsync(string postId, string text, CancellationToken cancellationToken = default)
        {
            return SendJsonAsync(
                HttpMethod.Post,
                "/api/add-text",
                () =>
                {
                    var content = new MultipartFormDataContent();
                    content.Add(new StringContent(postId), "post_id");
                    content.Add(new StringContent(text), "text");
                    return Task.FromResult<HttpContent>(content);
                },
                cancellationToken);
        }

        public Task AddImageAsync(string postId, IFormFile image, CancellationToken cancellationToken = default)
        {
            return SendNoContentOperationAsync(
                HttpMethod.Post,
                "/api/add-image",
                async () =>
                {
                    var bytes = await ReadFileBytesAsync(image, cancellationToken);
                    var content = new MultipartFormDataContent();
                    content.Add(new StringContent(postId), "post_id");
                    content.Add(CreateFileContent(bytes, image.ContentType), "image", image.FileName);
                    return content;
                },
                cancellationToken);
        }

        public Task AddFaceAsync(string personId, IFormFile image, CancellationToken cancellationToken = default)
        {
            return SendNoContentOperationAsync(
                HttpMethod.Post,
                "/api/add-face",
                async () =>
                {
                    var bytes = await ReadFileBytesAsync(image, cancellationToken);
                    var content = new MultipartFormDataContent();
                    content.Add(new StringContent(personId), "person_id");
                    content.Add(CreateFileContent(bytes, image.ContentType), "image", image.FileName);
                    return content;
                },
                cancellationToken);
        }

        public async Task<List<AiResultDto>> SearchTextAsync(string text, int k, CancellationToken cancellationToken = default)
        {
            var payload = await SendJsonAsync(
                HttpMethod.Post,
                "/api/search-text",
                () =>
                {
                    var content = new MultipartFormDataContent();
                    content.Add(new StringContent(text), "text");
                    content.Add(new StringContent(k.ToString()), "k");
                    return Task.FromResult<HttpContent>(content);
                },
                cancellationToken);

            return ParseResults(payload, "/api/search-text");
        }

        public async Task<List<AiResultDto>> SearchImageAsync(IFormFile image, int k, CancellationToken cancellationToken = default)
        {
            var bytes = await ReadFileBytesAsync(image, cancellationToken);
            var payload = await SendJsonAsync(
                HttpMethod.Post,
                "/api/search-image",
                () =>
                {
                    var content = new MultipartFormDataContent();
                    content.Add(new StringContent(k.ToString()), "k");
                    content.Add(CreateFileContent(bytes, image.ContentType), "image", image.FileName);
                    return Task.FromResult<HttpContent>(content);
                },
                cancellationToken);

            return ParseResults(payload, "/api/search-image");
        }

        public async Task<List<AiResultDto>> FaceMatchAsync(IFormFile image, int k, CancellationToken cancellationToken = default)
        {
            var bytes = await ReadFileBytesAsync(image, cancellationToken);
            var payload = await SendJsonAsync(
                HttpMethod.Post,
                "/api/face-match",
                () =>
                {
                    var content = new MultipartFormDataContent();
                    content.Add(new StringContent(k.ToString()), "k");
                    content.Add(CreateFileContent(bytes, image.ContentType), "image", image.FileName);
                    return Task.FromResult<HttpContent>(content);
                },
                cancellationToken);

            return ParseResults(payload, "/api/face-match");
        }

        public async Task<List<AiResultDto>> MultiModalSearchAsync(string? text, IFormFile? image, int k, CancellationToken cancellationToken = default)
        {
            byte[]? bytes = null;
            if (image is not null)
            {
                bytes = await ReadFileBytesAsync(image, cancellationToken);
            }

            var payload = await SendJsonAsync(
                HttpMethod.Post,
                "/api/multimodal-search",
                () =>
                {
                    var content = new MultipartFormDataContent();
                    content.Add(new StringContent(k.ToString()), "k");

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        content.Add(new StringContent(text), "text");
                    }

                    if (bytes is not null && image is not null)
                    {
                        content.Add(CreateFileContent(bytes, image.ContentType), "image", image.FileName);
                    }

                    return Task.FromResult<HttpContent>(content);
                },
                cancellationToken);

            return ParseResults(payload, "/api/multimodal-search");
        }

        public Task<JsonElement> GetHealthAsync(CancellationToken cancellationToken = default)
        {
            return SendJsonAsync(HttpMethod.Get, "/api/health", null, cancellationToken);
        }

        private async Task SendNoContentOperationAsync(
            HttpMethod method,
            string endpoint,
            Func<Task<HttpContent>> contentFactory,
            CancellationToken cancellationToken)
        {
            _ = await SendJsonAsync(method, endpoint, contentFactory, cancellationToken);
        }

        private async Task<JsonElement> SendJsonAsync(
            HttpMethod method,
            string endpoint,
            Func<Task<HttpContent>>? contentFactory,
            CancellationToken cancellationToken)
        {
            var body = await SendRequestAsync(method, endpoint, contentFactory, cancellationToken);
            return ParseJson(body, endpoint);
        }

        private async Task<string> SendRequestAsync(
            HttpMethod method,
            string endpoint,
            Func<Task<HttpContent>>? contentFactory,
            CancellationToken cancellationToken)
        {
            var delayMs = 300;
            var maxAttempts = MaxRetries + 1;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using var request = new HttpRequestMessage(method, endpoint);
                    if (contentFactory is not null)
                    {
                        request.Content = await contentFactory();
                    }

                    _logger.LogInformation(
                        "Sending AI request {Method} {Url} attempt {Attempt}/{MaxAttempts}",
                        method.Method,
                        request.RequestUri,
                        attempt,
                        maxAttempts);

                    using var response = await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                    var body = await response.Content.ReadAsStringAsync(cancellationToken);

                    _logger.LogInformation(
                        "Received AI response {Method} {Url} with status {StatusCode}",
                        method.Method,
                        request.RequestUri,
                        (int)response.StatusCode);

                    if (response.IsSuccessStatusCode)
                    {
                        return body;
                    }

                    var exception = new AiServiceException(
                        $"AI request to '{endpoint}' failed with status code {(int)response.StatusCode}.",
                        MapGatewayStatusCode(response.StatusCode),
                        endpoint,
                        body);

                    _logger.LogWarning(
                        exception,
                        "AI request {Method} {Url} failed with status {StatusCode}. Body: {Body}",
                        method.Method,
                        request.RequestUri,
                        (int)response.StatusCode,
                        body);

                    if (attempt == maxAttempts || !ShouldRetry(response.StatusCode))
                    {
                        throw exception;
                    }
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(
                        ex,
                        "AI request {Method} {Endpoint} timed out on attempt {Attempt}/{MaxAttempts}",
                        method.Method,
                        endpoint,
                        attempt,
                        maxAttempts);

                    if (attempt == maxAttempts)
                    {
                        throw new AiServiceException(
                            $"AI request to '{endpoint}' timed out after {_httpClient.Timeout.TotalSeconds} seconds.",
                            StatusCodes.Status504GatewayTimeout,
                            endpoint,
                            null,
                            ex);
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(
                        ex,
                        "AI request {Method} {Endpoint} failed on attempt {Attempt}/{MaxAttempts}",
                        method.Method,
                        endpoint,
                        attempt,
                        maxAttempts);

                    if (attempt == maxAttempts)
                    {
                        throw new AiServiceException(
                            $"AI request to '{endpoint}' failed before a response was received.",
                            StatusCodes.Status502BadGateway,
                            endpoint,
                            ex.Message,
                            ex);
                    }
                }

                await Task.Delay(delayMs, cancellationToken);
                delayMs = Math.Min(1000, delayMs * 2);
            }

            throw new AiServiceException(
                $"AI request to '{endpoint}' failed after all retry attempts.",
                StatusCodes.Status502BadGateway,
                endpoint);
        }

        private static bool ShouldRetry(HttpStatusCode statusCode)
        {
            var numericStatus = (int)statusCode;
            return numericStatus >= 500 || statusCode == HttpStatusCode.RequestTimeout || (int)statusCode == 429;
        }

        private static int MapGatewayStatusCode(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.RequestTimeout
                ? StatusCodes.Status504GatewayTimeout
                : StatusCodes.Status502BadGateway;
        }

        private static JsonElement ParseJson(string body, string endpoint)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                return document.RootElement.Clone();
            }
            catch (JsonException ex)
            {
                throw new AiServiceException(
                    $"AI response from '{endpoint}' was not valid JSON.",
                    StatusCodes.Status502BadGateway,
                    endpoint,
                    body,
                    ex);
            }
        }

        private static List<AiResultDto> ParseResults(JsonElement payload, string endpoint)
        {
            if (!payload.TryGetProperty("data", out var dataElement) ||
                !dataElement.TryGetProperty("results", out var resultsElement) ||
                resultsElement.ValueKind != JsonValueKind.Array)
            {
                throw new AiServiceException(
                    $"AI response from '{endpoint}' did not contain the expected 'data.results' array.",
                    StatusCodes.Status502BadGateway,
                    endpoint,
                    payload.GetRawText());
            }

            var results = new List<AiResultDto>();
            foreach (var item in resultsElement.EnumerateArray())
            {
                var score = item.TryGetProperty("score", out var scoreElement) && scoreElement.ValueKind == JsonValueKind.Number
                    ? scoreElement.GetDouble() * 100.0
                    : 0d;

                string postId = string.Empty;
                string? personId = null;
                string? text = null;

                if (item.TryGetProperty("metadata", out var metadataElement) && metadataElement.ValueKind == JsonValueKind.Object)
                {
                    if (metadataElement.TryGetProperty("post_id", out var postIdElement) && postIdElement.ValueKind == JsonValueKind.String)
                    {
                        postId = postIdElement.GetString() ?? string.Empty;
                    }

                    if (metadataElement.TryGetProperty("person_id", out var personIdElement) && personIdElement.ValueKind == JsonValueKind.String)
                    {
                        personId = personIdElement.GetString();
                    }

                    if (metadataElement.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
                    {
                        text = textElement.GetString();
                    }
                }

                if (!string.IsNullOrWhiteSpace(postId) || !string.IsNullOrWhiteSpace(personId))
                {
                    results.Add(new AiResultDto
                    {
                        Score = score,
                        PostId = postId,
                        PersonId = personId,
                        Text = text
                    });
                }
            }

            return results;
        }

        private static async Task<byte[]> ReadFileBytesAsync(IFormFile file, CancellationToken cancellationToken)
        {
            await using var stream = file.OpenReadStream();
            await using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            return memory.ToArray();
        }

        private static StreamContent CreateFileContent(byte[] bytes, string? contentType)
        {
            var content = new StreamContent(new MemoryStream(bytes, writable: false));
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            }

            return content;
        }
    }
}
