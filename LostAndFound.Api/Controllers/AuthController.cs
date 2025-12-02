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
    }
}