using Microsoft.AspNetCore.Http;

namespace LostAndFound.Application.Interfaces
{
    /// <summary>
    /// Centralized image upload/validation/deletion service.
    /// Removes duplicate upload logic from controllers.
    /// </summary>
    public interface IImageService
    {
        /// <summary>
        /// Validates and saves an image file to disk under wwwroot/uploads/{subFolder}.
        /// Returns the relative URL (e.g. "/uploads/profiles/1/abc.jpg").
        /// </summary>
        Task<string> SaveImageAsync(IFormFile file, string subFolder, long maxSizeBytes = 5 * 1024 * 1024);

        /// <summary>
        /// Deletes an image file from disk given its relative URL.
        /// No-op if the file does not exist.
        /// </summary>
        Task DeleteImageAsync(string relativeUrl);

        /// <summary>
        /// Validates that a file is an allowed image type and within size limits.
        /// Returns (isValid, errorMessage).
        /// </summary>
        (bool IsValid, string? ErrorMessage) ValidateImage(IFormFile file, long maxSizeBytes = 5 * 1024 * 1024);
    }
}
