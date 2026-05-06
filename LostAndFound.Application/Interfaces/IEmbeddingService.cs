namespace LostAndFound.Application.Interfaces
{
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generates an embedding vector for the input text.
        /// </summary>
        Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

        /// <inheritdoc cref="GenerateEmbeddingAsync"/>
        Task<float[]> EmbedTextAsync(string text, CancellationToken cancellationToken = default);
    }
}
