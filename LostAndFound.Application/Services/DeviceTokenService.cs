using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Application.Services
{
    public class DeviceTokenService : IDeviceTokenService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeviceTokenService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task RegisterTokenAsync(int userId, string token, string platform)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            var normalizedPlatform = platform?.ToLowerInvariant() ?? "android";
            if (normalizedPlatform != "android" && normalizedPlatform != "ios" && normalizedPlatform != "web")
                normalizedPlatform = "android";

            var existing = await _unitOfWork.DeviceTokens
                .FirstOrDefaultAsync(dt => dt.UserId == userId && dt.Token == token);

            if (existing != null)
            {
                existing.Platform = normalizedPlatform;
                existing.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.DeviceTokens.UpdateAsync(existing);
            }
            else
            {
                await _unitOfWork.DeviceTokens.AddAsync(new DeviceToken
                {
                    UserId = userId,
                    Token = token,
                    Platform = normalizedPlatform,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
