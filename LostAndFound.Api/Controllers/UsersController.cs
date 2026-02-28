using AutoMapper;
using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Common;
using LostAndFound.Application.DTOs.User;
using LostAndFound.Application.DTOs.Auth;
using LostAndFound.Application.DTOs.Report;
using LostAndFound.Application.Features.Users.Commands.CreateAdmin;
using LostAndFound.Application.Features.Users.Queries.GetAllUsers;
using LostAndFound.Application.Features.Users.Queries.GetUserById;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;
namespace LostAndFound.Api.Controllers
{
    /// <summary>
    /// Controller for managing users
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("api")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAuthService _authService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IImageService _imageService;
        private readonly IReportService _reportService;
        private readonly ISavedReportService _savedReportService;

        public UsersController(IMediator mediator, IAuthService authService, IUnitOfWork unitOfWork, IMapper mapper, IImageService imageService, IReportService reportService, ISavedReportService savedReportService)
        {
            _mediator = mediator;
            _authService = authService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _imageService = imageService;
            _reportService = reportService;
            _savedReportService = savedReportService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Get all users",
            Description = "Retrieves a paginated list of all users in the system. Requires Admin role. Supports search filtering by name or email."
        )]
        [ProducesResponseType(typeof(BaseResponse<PaginatedResponse<SafeUserDto>>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            var query = new GetAllUsersQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm
            };

            var result = await _mediator.Send(query);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Get user by ID",
            Description = "Retrieves user details by ID. Users can only view their own profile unless they are an Admin. Requires authentication."
        )]
        [ProducesResponseType(typeof(BaseResponse<SafeUserDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUser(int id)
        {
            // Get current user info from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                return Unauthorized(BaseResponse<SafeUserDto>.FailureResult("Invalid user token"));
            }

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            var query = new GetUserByIdQuery
            {
                Id = id,
                RequestingUserId = currentUserId,
                RequestingUserRole = userRole
            };

            var result = await _mediator.Send(query);
            
            if (result.Success)
            {
                return Ok(result);
            }

            // Handle specific error cases
            if (result.Message.Contains("Access denied"))
            {
                return StatusCode(403, BaseResponse<SafeUserDto>.FailureResult("You can only view your own profile"));
            }

            if (result.Message.Contains("not found"))
            {
                return NotFound(result);
            }
            
            return BadRequest(result);
        }

        [HttpGet("me")]
        [SwaggerOperation(
            Summary = "Get my data",
            Description = "Returns the authenticated user's profile information including name, email, phone, verification status, and roles. Requires authentication."
        )]
        [ProducesResponseType(typeof(BaseResponse<SafeUserDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(BaseResponse<SafeUserDto>.FailureResult("Invalid user token"));
            }

            var user = await _unitOfWork.Users.GetQueryable()
                .Where(u => u.Id == userId && !u.IsDeleted && !u.IsBlocked)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return Unauthorized(BaseResponse<SafeUserDto>.FailureResult("Authentication failed"));
            }

            var safeUser = new SafeUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                IsVerified = user.IsVerified,
                Roles = user.UserRoles?.Select(ur => ur.Role?.Name ?? string.Empty)
                    .Where(r => !string.IsNullOrEmpty(r)).ToList() ?? new List<string>(),
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            var safeResult = BaseResponse<SafeUserDto>.SuccessResult(safeUser, "User retrieved successfully");
            return Ok(safeResult);
        }

        [HttpPost("admin")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Create a new user",
            Description = "Creates a new admin user account. This endpoint is restricted to existing Admin users only. Requires Admin role."
        )]
        [ProducesResponseType(typeof(BaseResponse<UserDto>), 201)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminCommand command)
        {
            var result = await _mediator.Send(command);
            
            if (result.Success)
            {
                return CreatedAtAction(nameof(GetUser), new { id = result.Data!.Id }, result);
            }
            
            return BadRequest(result);
        }

        // Verify-user endpoint removed — use AdminController PUT /api/admin/users/{id}/verify instead.
        // This avoids duplicate endpoints with divergent behavior.

        [HttpPut("me")]
        [Consumes("multipart/form-data")]
        [EnableRateLimiting("upload")]
        [SwaggerOperation(
            Summary = "Update my data",
            Description = "Updates the authenticated user's profile information including name, phone, date of birth, gender, and profile picture. Accepts multipart/form-data. Requires authentication."
        )]
        [ProducesResponseType(typeof(BaseResponse<SafeUserDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto updateProfileDto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(BaseResponse<SafeUserDto>.FailureResult("Invalid user token"));
                }

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(BaseResponse<SafeUserDto>.FailureResult("Invalid request."));
                }

                // Update user properties if provided
                if (!string.IsNullOrWhiteSpace(updateProfileDto.FullName))
                {
                    user.FullName = updateProfileDto.FullName;
                }

                if (!string.IsNullOrWhiteSpace(updateProfileDto.Phone))
                {
                    user.Phone = updateProfileDto.Phone;
                }

                if (updateProfileDto.DateOfBirth.HasValue)
                {
                    user.DateOfBirth = updateProfileDto.DateOfBirth;
                }

                if (!string.IsNullOrWhiteSpace(updateProfileDto.Gender))
                {
                    // Validate gender value against the Gender enum
                    if (Enum.TryParse<Gender>(updateProfileDto.Gender, ignoreCase: true, out _))
                    {
                        user.Gender = updateProfileDto.Gender;
                    }
                    else
                    {
                        return BadRequest(BaseResponse<SafeUserDto>.FailureResult(
                            $"Invalid gender. Allowed values: {string.Join(", ", Enum.GetNames<Gender>())}"));
                    }
                }

                // Handle profile picture upload if provided (via IImageService)
                const long profilePictureMaxBytes = 2 * 1024 * 1024;
                if (updateProfileDto.ProfilePicture != null && updateProfileDto.ProfilePicture.Length > 0)
                {
                    var (isValid, errorMessage) = _imageService.ValidateImage(updateProfileDto.ProfilePicture, profilePictureMaxBytes);
                    if (!isValid)
                    {
                        return BadRequest(BaseResponse<SafeUserDto>.FailureResult(errorMessage ?? "Invalid image"));
                    }

                    // Delete old profile picture from disk
                    if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
                    {
                        await _imageService.DeleteImageAsync(user.ProfilePictureUrl);
                    }

                    user.ProfilePictureUrl = await _imageService.SaveImageAsync(updateProfileDto.ProfilePicture, $"profiles/{userId}", profilePictureMaxBytes);
                }

                user.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                var safeUser = new SafeUserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    IsVerified = user.IsVerified,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };

                return Ok(BaseResponse<SafeUserDto>.SuccessResult(safeUser, "Profile updated successfully"));
            }
            catch (Exception)
            {
                return StatusCode(500, BaseResponse<SafeUserDto>.FailureResult("An error occurred while updating the profile"));
            }
        }

        [HttpPost("me/profile-picture")]
        [Consumes("multipart/form-data")]
        [EnableRateLimiting("upload")]
        [SwaggerOperation(
            Summary = "Upload profile picture",
            Description = "Uploads a profile picture for the authenticated user. Accepts image files (jpg, jpeg, png, gif, webp) up to 5MB. Requires authentication."
        )]
        [ProducesResponseType(typeof(BaseResponse<object>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));
                }

                var (isValid, errorMessage) = _imageService.ValidateImage(file, 5 * 1024 * 1024);
                if (!isValid)
                {
                    return BadRequest(BaseResponse<object>.FailureResult(errorMessage!));
                }

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(BaseResponse<object>.FailureResult("Invalid request."));
                }

                // Delete old profile picture from disk
                if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
                {
                    await _imageService.DeleteImageAsync(user.ProfilePictureUrl);
                }

                var profilePictureUrl = await _imageService.SaveImageAsync(file, $"profiles/{userId}");

                user.ProfilePictureUrl = profilePictureUrl;
                user.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(BaseResponse<object>.SuccessResult(new { ProfilePictureUrl = profilePictureUrl }, "Profile picture uploaded successfully"));
            }
            catch (Exception)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult("An error occurred while uploading the profile picture"));
            }
        }

        [HttpGet("{id}/reports")]
        [SwaggerOperation(
            Summary = "Get user's reports",
            Description = "Retrieves all reports created by a specific user with pagination support. Requires authentication."
        )]
        [ProducesResponseType(typeof(BaseResponse<object>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUserReports(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                // Ownership check: only the user themselves or Admin can view reports
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                {
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));
                }

                var isAdmin = User.IsInRole("Admin");
                if (id != currentUserId && !isAdmin)
                {
                    return StatusCode(403, BaseResponse<object>.FailureResult("You can only view your own reports."));
                }

                if (page < 1)
                {
                    return BadRequest(BaseResponse<object>.FailureResult("Page number must be greater than 0"));
                }
                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(BaseResponse<object>.FailureResult("Page size must be between 1 and 100"));
                }

                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(BaseResponse<object>.FailureResult("Invalid request."));
                }

                var (reports, totalCount) = await _reportService.GetReportsByUserIdAsync(id, page, pageSize);

                return Ok(BaseResponse<object>.SuccessResult(new
                {
                    Reports = reports,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }, "User reports retrieved successfully"));
            }
            catch (Exception)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult("An error occurred while retrieving user reports"));
            }
        }

        [HttpGet("me/saved-reports")]
        [SwaggerOperation(
            Summary = "Get my saved reports",
            Description = "Retrieves all reports the authenticated user has saved."
        )]
        [ProducesResponseType(typeof(BaseResponse<object>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetMySavedReports()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));
                }

                var reports = await _savedReportService.GetSavedReportsAsync(userId);
                return Ok(BaseResponse<object>.SuccessResult(new { reports }, "Saved reports retrieved successfully"));
            }
            catch (Exception)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult("An error occurred while retrieving saved reports"));
            }
        }

    }
}