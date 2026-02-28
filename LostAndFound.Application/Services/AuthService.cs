using Google.Apis.Auth;
using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Auth;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace LostAndFound.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        private const int RefreshTokenExpiryDays = 7;

        public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, IEmailService emailService, IJwtService jwtService, ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _emailService = emailService;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<BaseResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto)
        {
            try
            {
                // Global query filter auto-excludes IsDeleted and IsBlocked users,
                // so a null result means user doesn't exist OR is deleted/blocked.
                var user = await _unitOfWork.Users.GetQueryable()
                    .Where(u => u.Email == loginDto.Email)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    // Perform a dummy hash to prevent timing attacks that reveal user existence
                    BCrypt.Net.BCrypt.Verify("dummy", BCrypt.Net.BCrypt.HashPassword("dummy"));
                    return BaseResponse<AuthResponseDto>.FailureResult("Invalid email or password");
                }

                if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    return BaseResponse<AuthResponseDto>.FailureResult("Invalid email or password");
                }

                if (!user.IsVerified)
                {
                    return BaseResponse<AuthResponseDto>.FailureResult("Please verify your account before logging in");
                }

                var userDto = MapToUserDto(user);
                var accessToken = await _jwtService.GenerateAccessTokenAsync(userDto);
                var refreshTokenStr = await _jwtService.GenerateRefreshTokenAsync();

                // Save refresh token to the new RefreshTokens table (multi-device)
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshTokenStr,
                    ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
                    CreatedAt = DateTime.UtcNow,
                    UserId = user.Id,
                    DeviceInfo = null // Can be set from request headers in the future
                };
                await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity);

                // Opportunistic cleanup: remove expired/revoked tokens for this user
                await CleanupExpiredTokensAsync(user.Id);

                // Keep legacy fields in sync for backward compatibility
                user.RefreshToken = refreshTokenStr;
                user.RefreshTokenExpiry = refreshTokenEntity.ExpiresAt;
                await _unitOfWork.SaveChangesAsync();

                var expiresAt = GetAccessTokenExpiry();

                var authResponse = new AuthResponseDto
                {
                    User = userDto,
                    AccessToken = accessToken,
                    RefreshToken = refreshTokenStr,
                    ExpiresAt = expiresAt
                };

                return BaseResponse<AuthResponseDto>.SuccessResult(authResponse, "Login successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", loginDto.Email);
                return BaseResponse<AuthResponseDto>.FailureResult("An unexpected error occurred.");
            }
        }

        public async Task<BaseResponse<AuthResponseDto>> GoogleSignInAsync(GoogleSignInDto dto)
        {
            try
            {
                // Support both Google:ClientId and GoogleAuth:ClientId (legacy) for config flexibility
                var clientId = _configuration["Google:ClientId"] ?? _configuration["GoogleAuth:ClientId"];
                if (string.IsNullOrEmpty(clientId))
                {
                    return BaseResponse<AuthResponseDto>.FailureResult("Google sign-in is not configured");
                }

                GoogleJsonWebSignature.Payload payload;
                try
                {
                    var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { clientId }
                    };
                    payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, validationSettings);
                }
                catch (InvalidJwtException)
                {
                    return BaseResponse<AuthResponseDto>.FailureResult("Invalid Google ID token");
                }

                var googleId = payload.Subject;
                var email = payload.Email;
                var name = payload.Name ?? (payload.GivenName != null && payload.FamilyName != null
                    ? $"{payload.GivenName} {payload.FamilyName}".Trim()
                    : payload.Email ?? "User");
                var picture = payload.Picture;

                // Use IgnoreQueryFilters to find blocked/deleted accounts linked to this Google ID,
                // so we can reject them instead of creating a duplicate user.
                var user = await _unitOfWork.Users.GetQueryable()
                    .IgnoreQueryFilters()
                    .Where(u => u.GoogleId == googleId || u.Email == email)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync();

                if (user != null)
                {
                    // Reject deleted/blocked users with generic message
                    if (user.IsDeleted || user.IsBlocked)
                    {
                        return BaseResponse<AuthResponseDto>.FailureResult("Authentication failed");
                    }
                    // Link Google account if they signed up with email first
                    if (string.IsNullOrEmpty(user.GoogleId))
                    {
                        user.GoogleId = googleId;
                        if (string.IsNullOrEmpty(user.ProfilePictureUrl) && !string.IsNullOrEmpty(picture))
                            user.ProfilePictureUrl = picture;
                        user.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.SaveChangesAsync();
                    }
                }
                else
                {
                    user = new AppUser
                    {
                        GoogleId = googleId,
                        FullName = name,
                        Email = email ?? throw new InvalidOperationException("Google payload missing email"),
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))),
                        Phone = string.Empty,
                        IsVerified = true,
                        ProfilePictureUrl = picture,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Users.AddAsync(user);
                    await _unitOfWork.SaveChangesAsync();

                    var userRole = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == "User");
                    if (userRole != null)
                    {
                        await _unitOfWork.UserRoles.AddAsync(new UserRole
                        {
                            UserId = user.Id,
                            RoleId = userRole.Id,
                            CreatedAt = DateTime.UtcNow
                        });
                        await _unitOfWork.SaveChangesAsync();
                    }

                    user = await _unitOfWork.Users.GetQueryable()
                        .Where(u => u.Id == user.Id)
                        .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync() ?? user;
                }

                var userDto = MapToUserDto(user);
                var accessToken = await _jwtService.GenerateAccessTokenAsync(userDto);
                var refreshTokenStr = await _jwtService.GenerateRefreshTokenAsync();

                // Multi-device refresh token
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshTokenStr,
                    ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
                    CreatedAt = DateTime.UtcNow,
                    UserId = user.Id
                };
                await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity);

                // Keep legacy fields in sync
                user.RefreshToken = refreshTokenStr;
                user.RefreshTokenExpiry = refreshTokenEntity.ExpiresAt;
                await _unitOfWork.SaveChangesAsync();

                var authResponse = new AuthResponseDto
                {
                    User = userDto,
                    AccessToken = accessToken,
                    RefreshToken = refreshTokenStr,
                    ExpiresAt = GetAccessTokenExpiry()
                };

                return BaseResponse<AuthResponseDto>.SuccessResult(authResponse, "Signed in with Google successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google sign-in failed");
                return BaseResponse<AuthResponseDto>.FailureResult("An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Registers a new user account. Does NOT issue JWT/refresh tokens —
        /// the user must verify their email first via VerifyAccountAsync, then log in.
        /// </summary>
        public async Task<BaseResponse> SignupAsync(SignupDto signupDto)
        {
            try
            {
                var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == signupDto.Email);
                if (existingUser != null)
                {
                    return BaseResponse.FailureResult("User with this email already exists");
                }

                var verificationCode = GenerateVerificationCode();
                var user = new AppUser
                {
                    FullName = $"{signupDto.FirstName.Trim()} {signupDto.LastName.Trim()}".Trim(),
                    Email = signupDto.Email,
                    Phone = signupDto.Phone ?? string.Empty,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(signupDto.Password),
                    IsVerified = false,
                    VerificationCode = verificationCode,
                    VerificationCodeExpiry = DateTime.UtcNow.AddHours(24),
                    CreatedAt = DateTime.UtcNow,
                    DateOfBirth = signupDto.DateOfBirth,
                    Gender = signupDto.Gender
                };

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                var userRole = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == "User");
                if (userRole != null)
                {
                    var newUserRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = userRole.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.UserRoles.AddAsync(newUserRole);
                    await _unitOfWork.SaveChangesAsync();
                }

                await _emailService.SendVerificationCodeEmailAsync(user.Email, user.FullName, verificationCode);

                // Security: Do NOT issue JWT or refresh token before email verification.
                // The user must verify their email, then log in to receive tokens.
                return BaseResponse.SuccessResult("Registration successful. Please check your email to verify your account.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Email}", signupDto.Email);
                return BaseResponse.FailureResult("An unexpected error occurred.");
            }
        }

        public async Task<BaseResponse> VerifyAccountAsync(VerifyAccountDto verifyAccountDto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == verifyAccountDto.Email);
                if (user == null)
                {
                    // Generic message — do not confirm whether the email exists
                    return BaseResponse.FailureResult("Invalid or expired verification code");
                }

                if (user.VerificationCode != verifyAccountDto.Code || 
                    user.VerificationCodeExpiry == null ||
                    user.VerificationCodeExpiry <= DateTime.UtcNow)
                {
                    return BaseResponse.FailureResult("Invalid or expired verification code");
                }

                user.IsVerified = true;
                user.VerificationCode = null;
                user.VerificationCodeExpiry = null;
                await _unitOfWork.SaveChangesAsync();

                return BaseResponse.SuccessResult("Account verified successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account verification failed");
                return BaseResponse.FailureResult("An unexpected error occurred.");
            }
        }

        public async Task<BaseResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            try
            {
                // Look up the token in the RefreshTokens table (multi-device)
                // Use IgnoreQueryFilters so the included User is loaded even if blocked/deleted,
                // allowing us to explicitly reject and revoke their token.
                var storedToken = await _unitOfWork.RefreshTokens.GetQueryable()
                    .IgnoreQueryFilters()
                    .Where(rt => rt.Token == refreshTokenDto.RefreshToken && rt.RevokedAt == null)
                    .Include(rt => rt.User)
                        .ThenInclude(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync();

                if (storedToken == null || storedToken.ExpiresAt <= DateTime.UtcNow)
                {
                    return BaseResponse<AuthResponseDto>.FailureResult("Invalid or expired refresh token");
                }

                var user = storedToken.User;

                // Prevent deleted/blocked users from refreshing
                if (user.IsDeleted || user.IsBlocked)
                {
                    // Revoke this token
                    storedToken.RevokedAt = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync();
                    return BaseResponse<AuthResponseDto>.FailureResult("Authentication failed");
                }

                // Rotate: revoke old token, issue new one
                storedToken.RevokedAt = DateTime.UtcNow;

                var newRefreshTokenStr = await _jwtService.GenerateRefreshTokenAsync();
                var newRefreshTokenEntity = new RefreshToken
                {
                    Token = newRefreshTokenStr,
                    ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
                    CreatedAt = DateTime.UtcNow,
                    UserId = user.Id,
                    DeviceInfo = storedToken.DeviceInfo // preserve device info
                };
                await _unitOfWork.RefreshTokens.AddAsync(newRefreshTokenEntity);

                // Opportunistic cleanup: remove expired/revoked tokens for this user
                await CleanupExpiredTokensAsync(user.Id);

                // Keep legacy fields in sync
                user.RefreshToken = newRefreshTokenStr;
                user.RefreshTokenExpiry = newRefreshTokenEntity.ExpiresAt;
                await _unitOfWork.SaveChangesAsync();

                var userDto = MapToUserDto(user);
                var accessToken = await _jwtService.GenerateAccessTokenAsync(userDto);

                var authResponse = new AuthResponseDto
                {
                    User = userDto,
                    AccessToken = accessToken,
                    RefreshToken = newRefreshTokenStr,
                    ExpiresAt = GetAccessTokenExpiry()
                };

                return BaseResponse<AuthResponseDto>.SuccessResult(authResponse, "Token refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return BaseResponse<AuthResponseDto>.FailureResult("An unexpected error occurred.");
            }
        }

        public async Task<BaseResponse> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == forgotPasswordDto.Email);
                if (user == null)
                {
                    // Return success even if user not found (security best practice)
                    return BaseResponse.SuccessResult("If the email exists, a password reset link has been sent");
                }

                // Generate password reset token
                var resetToken = GenerateVerificationCode(); // Reuse the same 6-digit code generator
                user.PasswordResetToken = resetToken;
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // 1 hour expiry
                await _unitOfWork.SaveChangesAsync();

                // Send password reset email
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetToken);

                return BaseResponse.SuccessResult("If the email exists, a password reset link has been sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email");
                return BaseResponse.FailureResult("An unexpected error occurred.");
            }
        }

        public async Task<BaseResponse> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == resetPasswordDto.Email);
                if (user == null)
                {
                    return BaseResponse.FailureResult("Invalid reset token");
                }

                if (user.PasswordResetToken != resetPasswordDto.ResetToken ||
                    user.PasswordResetTokenExpiry == null ||
                    user.PasswordResetTokenExpiry <= DateTime.UtcNow)
                {
                    return BaseResponse.FailureResult("Invalid or expired reset token");
                }

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(resetPasswordDto.NewPassword);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
                
                // Invalidate ALL refresh tokens for security (multi-device)
                await RevokeAllUserRefreshTokensAsync(user.Id);

                // Clear legacy fields
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                
                await _unitOfWork.SaveChangesAsync();

                return BaseResponse.SuccessResult("Password has been reset successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset failed");
                return BaseResponse.FailureResult("An unexpected error occurred.");
            }
        }

        public async Task<BaseResponse> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return BaseResponse.FailureResult("Authentication failed");
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
                {
                    return BaseResponse.FailureResult("Current password is incorrect");
                }

                // Update to new password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
                
                // Invalidate ALL refresh tokens for security (multi-device)
                await RevokeAllUserRefreshTokensAsync(user.Id);

                // Clear legacy fields
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                
                await _unitOfWork.SaveChangesAsync();

                return BaseResponse.SuccessResult("Password changed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password change failed for user {UserId}", userId);
                return BaseResponse.FailureResult("An unexpected error occurred.");
            }
        }

        public async Task<BaseResponse> ResendVerificationAsync(ResendVerificationDto resendVerificationDto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == resendVerificationDto.Email);
                if (user == null)
                {
                    // Generic message — do not confirm whether the email exists
                    return BaseResponse.SuccessResult("If the email exists and is not yet verified, a new verification code has been sent");
                }

                if (user.IsVerified)
                {
                    // Same generic message — do not reveal that the account is already verified
                    return BaseResponse.SuccessResult("If the email exists and is not yet verified, a new verification code has been sent");
                }

                // Generate new verification code
                var verificationCode = GenerateVerificationCode();
                user.VerificationCode = verificationCode;
                user.VerificationCodeExpiry = DateTime.UtcNow.AddHours(24);
                await _unitOfWork.SaveChangesAsync();

                // Send verification email
                await _emailService.SendVerificationCodeEmailAsync(user.Email, user.FullName, verificationCode);

                return BaseResponse.SuccessResult("If the email exists and is not yet verified, a new verification code has been sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend verification code");
                return BaseResponse.FailureResult("An unexpected error occurred.");
            }
        }

        public async Task<BaseResponse> LogoutAsync(int userId, LogoutDto logoutDto)
        {
            try
            {
                // Revoke the specific refresh token in the new table
                var storedToken = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(
                    rt => rt.UserId == userId && rt.Token == logoutDto.RefreshToken && rt.RevokedAt == null);

                if (storedToken != null)
                {
                    storedToken.RevokedAt = DateTime.UtcNow;
                }

                // Also clear legacy fields if the token matches
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null && user.RefreshToken == logoutDto.RefreshToken)
                {
                    user.RefreshToken = null;
                    user.RefreshTokenExpiry = null;
                }

                await _unitOfWork.SaveChangesAsync();

                return BaseResponse.SuccessResult("Logged out successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed for user {UserId}", userId);
                return BaseResponse.FailureResult("An unexpected error occurred.");
            }
        }

        public async Task<BaseResponse<UserDto>> GetCurrentUserAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetQueryable()
                    .Where(u => u.Id == userId && !u.IsDeleted && !u.IsBlocked)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return BaseResponse<UserDto>.FailureResult("Authentication failed");
                }

                var userDto = MapToUserDto(user);
                return BaseResponse<UserDto>.SuccessResult(userDto, "User retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user {UserId}", userId);
                return BaseResponse<UserDto>.FailureResult("An unexpected error occurred.");
            }
        }

        public async Task<BaseResponse> RequestEmailChangeAsync(int userId, ChangeEmailRequestDto dto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return BaseResponse.FailureResult("Authentication failed");
                }

                var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == dto.NewEmail);
                if (existingUser != null)
                {
                    return BaseResponse.FailureResult("Email address is already in use");
                }

                var verificationCode = GenerateVerificationCode();
                user.EmailChangeToken = verificationCode;
                user.EmailChangeTokenExpiry = DateTime.UtcNow.AddHours(24);
                user.PendingEmail = dto.NewEmail;
                await _unitOfWork.SaveChangesAsync();

                await _emailService.SendEmailChangeVerificationAsync(dto.NewEmail, user.FullName, verificationCode, dto.NewEmail);

                return BaseResponse.SuccessResult("Verification code has been sent to your new email address");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request email change for user {UserId}", userId);
                return BaseResponse.FailureResult("An unexpected error occurred.");
            }
        }

        public async Task<BaseResponse> ConfirmEmailChangeAsync(int userId, ChangeEmailConfirmDto dto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return BaseResponse.FailureResult("Authentication failed");
                }

                if (user.EmailChangeToken != dto.VerificationCode ||
                    user.EmailChangeTokenExpiry == null ||
                    user.EmailChangeTokenExpiry <= DateTime.UtcNow)
                {
                    return BaseResponse.FailureResult("Invalid or expired verification code");
                }

                if (string.IsNullOrEmpty(user.PendingEmail))
                {
                    return BaseResponse.FailureResult("No pending email change request");
                }

                user.Email = user.PendingEmail;
                user.EmailChangeToken = null;
                user.EmailChangeTokenExpiry = null;
                user.PendingEmail = null;

                // Invalidate ALL refresh tokens for security (multi-device)
                await RevokeAllUserRefreshTokensAsync(user.Id);
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;

                await _unitOfWork.SaveChangesAsync();

                return BaseResponse.SuccessResult("Email address has been changed successfully. Please log in again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to confirm email change for user {UserId}", userId);
                return BaseResponse.FailureResult("An unexpected error occurred.");
            }
        }

        public async Task<BaseResponse> DeleteAccountAsync(int userId, DeleteAccountDto dto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return BaseResponse.FailureResult("Authentication failed");
                }

                if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                {
                    return BaseResponse.FailureResult("Incorrect password");
                }

                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;

                // Invalidate ALL refresh tokens for security (multi-device)
                await RevokeAllUserRefreshTokensAsync(user.Id);
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;

                await _unitOfWork.SaveChangesAsync();

                return BaseResponse.SuccessResult("Your account has been deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete account for user {UserId}", userId);
                return BaseResponse.FailureResult("An unexpected error occurred.");
            }
        }

        // ── Helpers ──────────────────────────────────────────────

        /// <summary>
        /// Removes expired and revoked refresh tokens for a user to keep the DB clean.
        /// Called opportunistically during login and token refresh — no background service needed.
        /// </summary>
        private async Task CleanupExpiredTokensAsync(int userId)
        {
            try
            {
                var staleTokens = await _unitOfWork.RefreshTokens.GetQueryable()
                    .Where(rt => rt.UserId == userId &&
                        (rt.ExpiresAt < DateTime.UtcNow || rt.RevokedAt != null))
                    .ToListAsync();

                if (staleTokens.Count > 0)
                {
                    await _unitOfWork.RefreshTokens.DeleteRangeAsync(staleTokens);
                    // SaveChanges is called by the caller after this method
                }
            }
            catch (Exception ex)
            {
                // Non-critical: log and swallow so it never breaks login/refresh
                _logger.LogWarning(ex, "Token cleanup failed for user {UserId}", userId);
            }
        }

        /// <summary>
        /// Revokes all active refresh tokens for a given user (multi-device invalidation).
        /// </summary>
        private async Task RevokeAllUserRefreshTokensAsync(int userId)
        {
            var activeTokens = await _unitOfWork.RefreshTokens.GetQueryable()
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }
        }

        private DateTime GetAccessTokenExpiry()
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");
            return DateTime.UtcNow.AddMinutes(expiryMinutes);
        }

        private UserDto MapToUserDto(AppUser user)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                IsVerified = user.IsVerified,
                Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>(),
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        private string GenerateVerificationCode()
        {
            return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }
    }
}
