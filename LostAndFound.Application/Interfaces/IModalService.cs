using System.Text.Json;

namespace LostAndFound.Application.Interfaces
{
    public interface IModalService
    {
        /// <summary>
        /// Sends embedding plus Modal-required fields (index_name, k) to vector search. Never sends raw user text.
        /// </summary>
        /// <param name="indexNameOverride">Optional; defaults to Modal:DefaultIndexName.</param>
        Task<JsonElement> SearchByEmbeddingAsync(float[] embedding, string? indexNameOverride = null, CancellationToken cancellationToken = default);
    }
}
