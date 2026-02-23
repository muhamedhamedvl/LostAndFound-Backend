using LostAndFound.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LostAndFound.Application.Services
{
    /// <summary>
    /// Production image service: validates, saves to disk, deletes from disk.
    /// Centralizes logic previously duplicated in UsersController and PostsController.
    /// </summary>
    public class ImageService : IImageService
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly string _webRootPath;

        public ImageService(string webRootPath)
        {
            _webRootPath = webRootPath;
        }

        public (bool IsValid, string? ErrorMessage) ValidateImage(IFormFile file, long maxSizeBytes = 5 * 1024 * 1024)
        {
            if (file == null || file.Length == 0)
                return (false, "No file uploaded");

            var safeFileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(safeFileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
                return (false, $"Invalid file type. Allowed: {string.Join(", ", AllowedExtensions)}");

            if (file.Length > maxSizeBytes)
                return (false, $"File size exceeds {maxSizeBytes / (1024 * 1024)}MB limit");

            return (true, null);
        }

        public async Task<string> SaveImageAsync(IFormFile file, string subFolder, long maxSizeBytes = 5 * 1024 * 1024)
        {
            var (isValid, errorMessage) = ValidateImage(file, maxSizeBytes);
            if (!isValid)
                throw new ArgumentException(errorMessage);

            var safeFileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
            var uniqueName = $"{Guid.NewGuid()}{extension}";

            var uploadsDir = Path.Combine(_webRootPath, "uploads", subFolder);
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            var filePath = Path.Combine(uploadsDir, uniqueName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{subFolder}/{uniqueName}";
        }

        public Task DeleteImageAsync(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                return Task.CompletedTask;

            // Convert "/uploads/profiles/1/abc.jpg" → full disk path
            var relativePath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_webRootPath, relativePath);

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            return Task.CompletedTask;
        }
    }
}
