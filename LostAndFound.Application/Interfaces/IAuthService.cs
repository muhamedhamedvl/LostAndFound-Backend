using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Auth;

namespace LostAndFound.Application.Interfaces
{
    public interface IAuthService
    {
        Task<BaseResponse<AuthResponseDto>> SignupAsync(SignupDto signupDto);
        Task<BaseResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto);
        Task<BaseResponse> VerifyAccountAsync(VerifyAccountDto verifyAccountDto);
    }
}
