using LostAndFound.Application.DTOs.Auth;

namespace LostAndFound.Application.Interfaces
{
    public interface IJwtService
    {
        Task<string> GenerateAccessTokenAsync(UserDto user);
        Task<string> GenerateRefreshTokenAsync();
        Task<bool> ValidateRefreshTokenAsync(string refreshToken);
        Task<UserDto?> GetUserFromTokenAsync(string token);
    }
}
