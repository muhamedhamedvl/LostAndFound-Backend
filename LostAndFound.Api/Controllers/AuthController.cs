using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Auth;
using LostAndFound.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;
namespace LostAndFound.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("signup")]
        [SwaggerOperation(
            Summary = "Create a new user account",
            Description = "Registers a new user with email, password, and personal information. A verification code will be sent to the provided email address. No JWT is issued until the account is verified."
        )]
        public async Task<IActionResult> Signup([FromBody] SignupDto signupDto)
        {
            try
            {
                var result = await _authService.SignupAsync(signupDto);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signup");
                return StatusCode(500, BaseResponse.FailureResult("An unexpected error occurred."));
            }
        }

        [HttpPost("login")]
        [SwaggerOperation(
            Summary = "Authenticate user and get access token",
            Description = "Logs in a user with email and password. Returns a JWT access token for authenticated requests."
        )]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authService.LoginAsync(loginDto);
                return result.Success ? Ok(result) : Unauthorized(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, BaseResponse.FailureResult("An unexpected error occurred."));
            }
        }

        [HttpPost("google")]
        [SwaggerOperation(
            Summary = "Sign in with Google",
            Description = "Authenticates using a Google ID token (from Google Sign-In on mobile or web). Creates a new user if none exists for this Google account, or links to an existing account by email. Returns the same JWT access token and refresh token as login."
        )]
        public async Task<IActionResult> GoogleSignIn([FromBody] GoogleSignInDto dto)
        {
            try
            {
                var result = await _authService.GoogleSignInAsync(dto);
                return result.Success ? Ok(result) : Unauthorized(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google sign-in");
                return StatusCode(500, BaseResponse.FailureResult("An unexpected error occurred."));
            }
        }

        [HttpGet("verify-account")]
        [SwaggerOperation(
            Summary = "Verify user account with verification code",
            Description = "Verifies a user's email address using the verification code sent to their email. This endpoint does not require authentication."
        )]
        public async Task<IActionResult> VerifyAccount([FromQuery] string code, [FromQuery] string email)
        {
            try
            {
                var verifyAccountDto = new VerifyAccountDto
                {
                    Code = code,
                    Email = email
                };

                var result = await _authService.VerifyAccountAsync(verifyAccountDto);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during account verification");
                return StatusCode(500, BaseResponse.FailureResult("An unexpected error occurred."));
            }
        }

        [HttpPost("refresh-token")]
        [EnableRateLimiting("refresh")]
        [SwaggerOperation(
            Summary = "Refresh access token using a valid refresh token",
            Description = "Validates the provided refresh token and issues a new access token and refresh token pair. This allows users to maintain their session without re-authenticating."
        )]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(refreshTokenDto);
                return result.Success ? Ok(result) : Unauthorized(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, BaseResponse.FailureResult("An unexpected error occurred."));
            }
        }

        [HttpPost("forgot-password")]
        [SwaggerOperation(
            Summary = "Request a password reset email",
            Description = "Sends a password reset email with a secure token to the provided email address. The token is valid for 1 hour."
        )]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password");
                return StatusCode(500, BaseResponse.FailureResult("An unexpected error occurred."));
            }
        }

        [HttpPost("reset-password")]
        [SwaggerOperation(
            Summary = "Reset password using the token received via email",
            Description = "Validates the reset token and updates the user's password. All active refresh tokens are invalidated for security."
        )]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                var result = await _authService.ResetPasswordAsync(resetPasswordDto);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return StatusCode(500, BaseResponse.FailureResult("An unexpected error occurred."));
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Change password for authenticated user",
            Description = "Allows an authenticated user to change their password by providing the current password and a new password. All active refresh tokens are invalidated for security."
        )]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(BaseResponse.FailureResult("User not authenticated."));
                }

                var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change");
                return StatusCode(500, BaseResponse.FailureResult("An unexpected error occurred."));
            }
        }

        [HttpPost("resend-verification")]
        [SwaggerOperation(
            Summary = "Resend account verification code",
            Description = "Generates a new verification code and sends it to the user's email address. The code is valid for 24 hours."
        )]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto resendVerificationDto)
        {
            try
            {
                var result = await _authService.ResendVerificationAsync(resendVerificationDto);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during resend verification");
                return StatusCode(500, BaseResponse.FailureResult("An unexpected error occurred."));
            }
        }

        [HttpPost("logout")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Logout user and invalidate refresh token",
            Description = "Invalidates the user's refresh token to end the session. Requires authentication."
        )]
        public async Task<IActionResult> Logout([FromBody] LogoutDto logoutDto)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(BaseResponse.FailureResult("User not authenticated."));
                }

                var result = await _authService.LogoutAsync(userId, logoutDto);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, BaseResponse.FailureResult("An unexpected error occurred."));
            }
        }

        [HttpGet("me")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Get current authenticated user",
            Description = "Returns the profile information of the currently authenticated user. Requires authentication."
        )]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(BaseResponse.FailureResult("User not authenticated."));
                }

                var result = await _authService.GetCurrentUserAsync(userId);
                return result.Success ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, BaseResponse.FailureResult("An unexpected error occurred."));
            }
        }

        [HttpPost("change-email-request")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Request email change",
            Description = "Initiates an email change request. A verification code will be sent to the new email address. Requires authentication."
        )]
        public async Task<IActionResult> RequestEmailChange([FromBody] ChangeEmailRequestDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(BaseResponse.FailureResult("User not authenticated."));
                }

                var result = await _authService.RequestEmailChangeAsync(userId, dto);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email change request");
                return StatusCode(500, BaseResponse.FailureResult("An unexpected error occurred."));
            }
        }

        [HttpPost("change-email-confirm")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Confirm email change",
            Description = "Confirms the email change using the verification code sent to the new email. All active sessions will be invalidated. Requires authentication."
        )]
        public async Task<IActionResult> ConfirmEmailChange([FromBody] ChangeEmailConfirmDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(BaseResponse.FailureResult("User not authenticated."));
                }

                var result = await _authService.ConfirmEmailChangeAsync(userId, dto);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email change confirmation");
                return StatusCode(500, BaseResponse.FailureResult("An unexpected error occurred."));
            }
        }

        [HttpDelete("delete-account")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Delete user account",
            Description = "Permanently deletes the user's account (soft delete). Requires password confirmation for security. Requires authentication."
        )]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(BaseResponse.FailureResult("User not authenticated."));
                }

                var result = await _authService.DeleteAccountAsync(userId, dto);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during account deletion");
                return StatusCode(500, BaseResponse.FailureResult("An unexpected error occurred."));
            }
        }
    }
}
