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

                var jwtSettings = _configuration.GetSection("JwtSettings");
                var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");
                var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

                var authResponse = new AuthResponseDto
                {
                    User = userDto,
                    AccessToken = accessToken,
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

                var jwtSettings = _configuration.GetSection("JwtSettings");
                var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");
                var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

                var authResponse = new AuthResponseDto
                {
                    User = userDto,
                    AccessToken = accessToken,
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