using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Auth;

namespace LostAndFound.Application.Interfaces
{
    public interface IAuthService
    {
        Task<BaseResponse<AuthResponseDto>> SignupAsync(SignupDto signupDto);
        Task<BaseResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto);
        Task<BaseResponse> VerifyAccountAsync(VerifyAccountDto verifyAccountDto);
        Task<BaseResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
        Task<BaseResponse> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
        Task<BaseResponse> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task<BaseResponse> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
        Task<BaseResponse> ResendVerificationAsync(ResendVerificationDto resendVerificationDto);
        Task<BaseResponse> LogoutAsync(int userId, LogoutDto logoutDto);
        Task<BaseResponse<UserDto>> GetCurrentUserAsync(int userId);
        Task<BaseResponse> RequestEmailChangeAsync(int userId, ChangeEmailRequestDto dto);
        Task<BaseResponse> ConfirmEmailChangeAsync(int userId, ChangeEmailConfirmDto dto);
        Task<BaseResponse> DeleteAccountAsync(int userId, DeleteAccountDto dto);
    }
}
