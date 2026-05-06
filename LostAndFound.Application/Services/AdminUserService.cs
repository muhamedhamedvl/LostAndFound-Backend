using LostAndFound.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Application.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminUserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetQueryable()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return false;

            if (user.IsDeleted)
                return true;

            // Revoke all active sessions/tokens.
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.EmailChangeToken = null;
            user.EmailChangeTokenExpiry = null;
            user.PendingEmail = null;
            user.VerificationCode = null;
            user.VerificationCodeExpiry = null;

            var refreshTokens = await _unitOfWork.RefreshTokens.FindAsync(rt => rt.UserId == userId);
            foreach (var token in refreshTokens)
            {
                await _unitOfWork.RefreshTokens.DeleteAsync(token);
            }

            var deviceTokens = await _unitOfWork.DeviceTokens.FindAsync(dt => dt.UserId == userId);
            foreach (var token in deviceTokens)
            {
                await _unitOfWork.DeviceTokens.DeleteAsync(token);
            }

            // Safe delete for production to preserve FK integrity and historical data.
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.IsBlocked = true;

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
