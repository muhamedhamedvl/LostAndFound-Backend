using LostAndFound.Application.DTOs.Ai;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace LostAndFound.Application.Interfaces
{
    public interface IAiService
    {
        Task<JsonElement> AddTextAsync(string postId, string text, CancellationToken cancellationToken = default);
        Task AddImageAsync(string postId, IFormFile image, CancellationToken cancellationToken = default);
        Task AddFaceAsync(string personId, IFormFile image, CancellationToken cancellationToken = default);
        Task<List<AiResultDto>> SearchTextAsync(string text, int k, CancellationToken cancellationToken = default);
        Task<List<AiResultDto>> SearchImageAsync(IFormFile image, int k, CancellationToken cancellationToken = default);
        Task<List<AiResultDto>> FaceMatchAsync(IFormFile image, int k, CancellationToken cancellationToken = default);
        Task<List<AiResultDto>> MultiModalSearchAsync(string? text, IFormFile? image, int k, CancellationToken cancellationToken = default);
        Task<JsonElement> GetHealthAsync(CancellationToken cancellationToken = default);
    }
}
