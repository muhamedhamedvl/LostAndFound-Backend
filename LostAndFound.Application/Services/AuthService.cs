using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Auth;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LostAndFound.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IJwtService _jwtService;

        public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, IEmailService emailService, IJwtService jwtService)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _emailService = emailService;
            _jwtService = jwtService;
        }

        public async Task<BaseResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Login attempt for email: {loginDto.Email}");
                
                var user = await _unitOfWork.Users.GetQueryable()
                    .Where(u => u.Email == loginDto.Email)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync();

                Console.WriteLine($"[DEBUG] User found: {user != null}");
                if (user != null)
                {
                    Console.WriteLine($"[DEBUG] User ID: {user.Id}");
                    Console.WriteLine($"[DEBUG] User Email: {user.Email}");
                    Console.WriteLine($"[DEBUG] User IsVerified: {user.IsVerified}");
                    Console.WriteLine($"[DEBUG] Password Hash (first 30 chars): {user.PasswordHash?.Substring(0, Math.Min(30, user.PasswordHash.Length))}...");
                    Console.WriteLine($"[DEBUG] Attempting BCrypt verification...");
                    
                    bool passwordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
                    Console.WriteLine($"[DEBUG] BCrypt verification result: {passwordValid}");
                }

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    Console.WriteLine($"[DEBUG] Login failed - Invalid credentials");
                    return BaseResponse<AuthResponseDto>.FailureResult("Invalid email or password");
                }

                if (!user.IsVerified)
                {
                    return BaseResponse<AuthResponseDto>.FailureResult("Please verify your account before logging in");
                }

                var userDto = MapToUserDto(user);
                var accessToken = await _jwtService.GenerateAccessTokenAsync(userDto);
                var refreshToken = await _jwtService.GenerateRefreshTokenAsync();

                // Save refresh token to database
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7); // 7 days expiry
                await _unitOfWork.SaveChangesAsync();

                var jwtSettings = _configuration.GetSection("JwtSettings");
                var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");
                var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

                var authResponse = new AuthResponseDto
                {
                    User = userDto,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt
                };

                return BaseResponse<AuthResponseDto>.SuccessResult(authResponse, "Login successful");
            }
            catch (Exception ex)
            {
                return BaseResponse<AuthResponseDto>.FailureResult($"Login failed: {ex.Message}");
            }
        }

        public async Task<BaseResponse<AuthResponseDto>> SignupAsync(SignupDto signupDto)
        {
            try
            {
                var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == signupDto.Email);
                if (existingUser != null)
                {
                    return BaseResponse<AuthResponseDto>.FailureResult("User with this email already exists");
                }

                var verificationCode = GenerateVerificationCode();
                var user = new User
                {
                    FullName = signupDto.FullName,
                    Email = signupDto.Email,
                    Phone = signupDto.Phone ?? string.Empty,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(signupDto.Password),
                    IsVerified = false,
                    VerificationCode = verificationCode,
                    VerificationCodeExpiry = DateTime.UtcNow.AddHours(24),
                    CreatedAt = DateTime.UtcNow
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

                user = await _unitOfWork.Users.GetQueryable()
                    .Where(u => u.Id == user.Id)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync() ?? user;

                var userDto = MapToUserDto(user);
                var accessToken = await _jwtService.GenerateAccessTokenAsync(userDto);
                var refreshToken = await _jwtService.GenerateRefreshTokenAsync();

                // Save refresh token to database
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7); // 7 days expiry
                await _unitOfWork.SaveChangesAsync();

                var jwtSettings = _configuration.GetSection("JwtSettings");
                var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");
                var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

                var authResponse = new AuthResponseDto
                {
                    User = userDto,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt
                };

                return BaseResponse<AuthResponseDto>.SuccessResult(authResponse, "Registration successful. Please check your email to verify your account.");
            }
            catch (Exception ex)
            {
                return BaseResponse<AuthResponseDto>.FailureResult($"Registration failed: {ex.Message}");
            }
        }
        public async Task<BaseResponse> VerifyAccountAsync(VerifyAccountDto verifyAccountDto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == verifyAccountDto.Email);
                if (user == null)
                {
                    return BaseResponse.FailureResult("User not found");
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
                return BaseResponse.FailureResult($"Account verification failed: {ex.Message}");
            }
        }

        public async Task<BaseResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var user = await _unitOfWork.Users.GetQueryable()
                    .Where(u => u.RefreshToken == refreshTokenDto.RefreshToken)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return BaseResponse<AuthResponseDto>.FailureResult("Invalid refresh token");
                }

                if (user.RefreshTokenExpiry == null || user.RefreshTokenExpiry <= DateTime.UtcNow)
                {
                    return BaseResponse<AuthResponseDto>.FailureResult("Refresh token has expired");
                }

                // Generate new tokens
                var userDto = MapToUserDto(user);
                var accessToken = await _jwtService.GenerateAccessTokenAsync(userDto);
                var newRefreshToken = await _jwtService.GenerateRefreshTokenAsync();

                // Update refresh token in database
                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                await _unitOfWork.SaveChangesAsync();

                var jwtSettings = _configuration.GetSection("JwtSettings");
                var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");
                var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

                var authResponse = new AuthResponseDto
                {
                    User = userDto,
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = expiresAt
                };

                return BaseResponse<AuthResponseDto>.SuccessResult(authResponse, "Token refreshed successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<AuthResponseDto>.FailureResult($"Token refresh failed: {ex.Message}");
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
                return BaseResponse.FailureResult($"Failed to send password reset email: {ex.Message}");
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
                
                // Invalidate all refresh tokens for security
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                
                await _unitOfWork.SaveChangesAsync();

                return BaseResponse.SuccessResult("Password has been reset successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse.FailureResult($"Password reset failed: {ex.Message}");
            }
        }

        public async Task<BaseResponse> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return BaseResponse.FailureResult("User not found");
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
                {
                    return BaseResponse.FailureResult("Current password is incorrect");
                }

                // Update to new password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
                
                // Invalidate all refresh tokens for security
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                
                await _unitOfWork.SaveChangesAsync();

                return BaseResponse.SuccessResult("Password changed successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse.FailureResult($"Password change failed: {ex.Message}");
            }
        }

        public async Task<BaseResponse> ResendVerificationAsync(ResendVerificationDto resendVerificationDto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == resendVerificationDto.Email);
                if (user == null)
                {
                    return BaseResponse.FailureResult("User not found");
                }

                if (user.IsVerified)
                {
                    return BaseResponse.FailureResult("Account is already verified");
                }

                // Generate new verification code
                var verificationCode = GenerateVerificationCode();
                user.VerificationCode = verificationCode;
                user.VerificationCodeExpiry = DateTime.UtcNow.AddHours(24);
                await _unitOfWork.SaveChangesAsync();

                // Send verification email
                await _emailService.SendVerificationCodeEmailAsync(user.Email, user.FullName, verificationCode);

                return BaseResponse.SuccessResult("Verification code has been resent to your email");
            }
            catch (Exception ex)
            {
                return BaseResponse.FailureResult($"Failed to resend verification code: {ex.Message}");
            }
        }

        public async Task<BaseResponse> LogoutAsync(int userId, LogoutDto logoutDto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return BaseResponse.FailureResult("User not found");
                }

                // Verify that the refresh token matches
                if (user.RefreshToken != logoutDto.RefreshToken)
                {
                    return BaseResponse.FailureResult("Invalid refresh token");
                }

                // Invalidate the refresh token
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                await _unitOfWork.SaveChangesAsync();

                return BaseResponse.SuccessResult("Logged out successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse.FailureResult($"Logout failed: {ex.Message}");
            }
        }

        public async Task<BaseResponse<UserDto>> GetCurrentUserAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetQueryable()
                    .Where(u => u.Id == userId)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return BaseResponse<UserDto>.FailureResult("User not found");
                }

                var userDto = MapToUserDto(user);
                return BaseResponse<UserDto>.SuccessResult(userDto, "User retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<UserDto>.FailureResult($"Failed to retrieve user: {ex.Message}");
            }
        }

        public async Task<BaseResponse> RequestEmailChangeAsync(int userId, ChangeEmailRequestDto dto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return BaseResponse.FailureResult("User not found");
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
                return BaseResponse.FailureResult($"Failed to request email change: {ex.Message}");
            }
        }

        public async Task<BaseResponse> ConfirmEmailChangeAsync(int userId, ChangeEmailConfirmDto dto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return BaseResponse.FailureResult("User not found");
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
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;

                await _unitOfWork.SaveChangesAsync();

                return BaseResponse.SuccessResult("Email address has been changed successfully. Please log in again.");
            }
            catch (Exception ex)
            {
                return BaseResponse.FailureResult($"Failed to confirm email change: {ex.Message}");
            }
        }

        public async Task<BaseResponse> DeleteAccountAsync(int userId, DeleteAccountDto dto)
        {
            try
            {
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return BaseResponse.FailureResult("User not found");
                }

                if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                {
                    return BaseResponse.FailureResult("Incorrect password");
                }

                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;

                await _unitOfWork.SaveChangesAsync();

                return BaseResponse.SuccessResult("Your account has been deleted successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse.FailureResult($"Failed to delete account: {ex.Message}");
            }
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                IsVerified = user.IsVerified,
                Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>(),
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        private string GenerateVerificationCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}