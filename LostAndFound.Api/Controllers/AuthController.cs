using LostAndFound.Application.DTOs.Auth;
using LostAndFound.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
namespace LostAndFound.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("signup")]
        [SwaggerOperation(
            Summary = "Create a new user account",
            Description = "Registers a new user with email, password, and personal information. A verification code will be sent to the provided email address."
        )]
        public async Task<IActionResult> Signup([FromBody] SignupDto signupDto)
        {
            var result = await _authService.SignupAsync(signupDto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpPost("login")]
        [SwaggerOperation(
            Summary = "Authenticate user and get access token",
            Description = "Logs in a user with email and password. Returns a JWT access token for authenticated requests."
        )]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return Unauthorized(result);
        }

        [HttpGet("verify-account")]
        [SwaggerOperation(
            Summary = "Verify user account with verification code",
            Description = "Verifies a user's email address using the verification code sent to their email. This endpoint does not require authentication."
        )]
        public async Task<IActionResult> VerifyAccount([FromQuery] string code, [FromQuery] string email)
        {
            var verifyAccountDto = new VerifyAccountDto
            {
                Code = code,
                Email = email
            };

            var result = await _authService.VerifyAccountAsync(verifyAccountDto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpPost("refresh-token")]
        [SwaggerOperation(
            Summary = "Refresh access token using a valid refresh token",
            Description = "Validates the provided refresh token and issues a new access token and refresh token pair. This allows users to maintain their session without re-authenticating."
        )]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            var result = await _authService.RefreshTokenAsync(refreshTokenDto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return Unauthorized(result);
        }

        [HttpPost("forgot-password")]
        [SwaggerOperation(
            Summary = "Request a password reset email",
            Description = "Sends a password reset email with a secure token to the provided email address. The token is valid for 1 hour."
        )]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpPost("reset-password")]
        [SwaggerOperation(
            Summary = "Reset password using the token received via email",
            Description = "Validates the reset token and updates the user's password. All active refresh tokens are invalidated for security."
        )]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            var result = await _authService.ResetPasswordAsync(resetPasswordDto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpPost("change-password")]
        [SwaggerOperation(
            Summary = "Change password for authenticated user",
            Description = "Allows an authenticated user to change their password by providing the current password and a new password. All active refresh tokens are invalidated for security."
        )]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpPost("resend-verification")]
        [SwaggerOperation(
            Summary = "Resend account verification code",
            Description = "Generates a new verification code and sends it to the user's email address. The code is valid for 24 hours."
        )]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto resendVerificationDto)
        {
            var result = await _authService.ResendVerificationAsync(resendVerificationDto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpPost("logout")]
        [SwaggerOperation(
            Summary = "Logout user and invalidate refresh token",
            Description = "Invalidates the user's refresh token to end the session. Requires authentication."
        )]
        public async Task<IActionResult> Logout([FromBody] LogoutDto logoutDto)
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            var result = await _authService.LogoutAsync(userId, logoutDto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpGet("me")]
        [SwaggerOperation(
            Summary = "Get current authenticated user",
            Description = "Returns the profile information of the currently authenticated user. Requires authentication."
        )]
        public async Task<IActionResult> GetCurrentUser()
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            var result = await _authService.GetCurrentUserAsync(userId);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return NotFound(result);
        }

        [HttpPost("change-email-request")]
        [SwaggerOperation(
            Summary = "Request email change",
            Description = "Initiates an email change request. A verification code will be sent to the new email address. Requires authentication."
        )]
        public async Task<IActionResult> RequestEmailChange([FromBody] ChangeEmailRequestDto dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            var result = await _authService.RequestEmailChangeAsync(userId, dto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpPost("change-email-confirm")]
        [SwaggerOperation(
            Summary = "Confirm email change",
            Description = "Confirms the email change using the verification code sent to the new email. All active sessions will be invalidated. Requires authentication."
        )]
        public async Task<IActionResult> ConfirmEmailChange([FromBody] ChangeEmailConfirmDto dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            var result = await _authService.ConfirmEmailChangeAsync(userId, dto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpDelete("delete-account")]
        [SwaggerOperation(
            Summary = "Delete user account",
            Description = "Permanently deletes the user's account (soft delete). Requires password confirmation for security. Requires authentication."
        )]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDto dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            var result = await _authService.DeleteAccountAsync(userId, dto);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
    }
}