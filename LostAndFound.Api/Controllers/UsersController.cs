using AutoMapper;
using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Common;
using LostAndFound.Application.DTOs.User;
using LostAndFound.Application.DTOs.Auth;
using LostAndFound.Application.DTOs.Post;
using LostAndFound.Application.Features.Users.Commands.CreateAdmin;
using LostAndFound.Application.Features.Users.Queries.GetAllUsers;
using LostAndFound.Application.Features.Users.Queries.GetUserById;
using LostAndFound.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
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
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAuthService _authService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UsersController(IMediator mediator, IAuthService authService, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _mediator = mediator;
            _authService = authService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
            Description = "Returns the authenticated user's profile information including name, email, phone, and verification status. Requires authentication."
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

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(BaseResponse<SafeUserDto>.FailureResult("User not found"));
            }

            var safeUser = new SafeUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                IsVerified = user.IsVerified,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            var safeResult = BaseResponse<SafeUserDto>.SuccessResult(safeUser, "User retrieved successfully");
            return Ok(safeResult);
        }

        [HttpPost("admin")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Create a new user (worker)",
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

        [HttpPut("me")]
        [Consumes("multipart/form-data")]
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
                    return NotFound(BaseResponse<SafeUserDto>.FailureResult("User not found"));
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
                    // Validate gender value
                    var validGenders = new[] { "Male", "Female", "Anonymous" };
                    if (validGenders.Contains(updateProfileDto.Gender))
                    {
                        user.Gender = updateProfileDto.Gender;
                    }
                    else
                    {
                        return BadRequest(BaseResponse<SafeUserDto>.FailureResult("Invalid gender. Allowed values: Male, Female, Anonymous"));
                    }
                }

                // Handle profile picture upload if provided
                if (updateProfileDto.ProfilePicture != null && updateProfileDto.ProfilePicture.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(updateProfileDto.ProfilePicture.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return BadRequest(BaseResponse<SafeUserDto>.FailureResult("Invalid file type. Allowed: .jpg, .jpeg, .png, .gif, .webp"));
                    }

                    if (updateProfileDto.ProfilePicture.Length > 2 * 1024 * 1024)
                    {
                        return BadRequest(BaseResponse<SafeUserDto>.FailureResult("File size exceeds 2MB limit"));
                    }

                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile-pictures");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    var fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await updateProfileDto.ProfilePicture.CopyToAsync(stream);
                    }

                    var profilePictureUrl = $"/uploads/profile-pictures/{fileName}";
                    user.ProfilePictureUrl = profilePictureUrl;
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
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<SafeUserDto>.FailureResult($"Error updating profile: {ex.Message}"));
            }
        }

        [HttpPost("me/profile-picture")]
        [Consumes("multipart/form-data")]
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

                if (file == null || file.Length == 0)
                {
                    return BadRequest(BaseResponse<object>.FailureResult("No file uploaded"));
                }

                // Validate file type and size
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(BaseResponse<object>.FailureResult("Invalid file type. Allowed: .jpg, .jpeg, .png, .gif, .webp"));
                }

                if (file.Length > 5 * 1024 * 1024) // 5MB
                {
                    return BadRequest(BaseResponse<object>.FailureResult("File size exceeds 5MB limit"));
                }

                // Get user
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(BaseResponse<object>.FailureResult("User not found"));
                }

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var profilePictureUrl = $"/uploads/profiles/{userId}/{fileName}";

                user.ProfilePictureUrl = profilePictureUrl;
                user.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(BaseResponse<object>.SuccessResult(new { ProfilePictureUrl = profilePictureUrl }, "Profile picture uploaded successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error uploading profile picture: {ex.Message}"));
            }
        }

        [HttpGet("{id}/posts")]
        [SwaggerOperation(
            Summary = "Get user's posts",
            Description = "Retrieves all posts created by a specific user with pagination support. Requires authentication."
        )]
        [ProducesResponseType(typeof(BaseResponse<object>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUserPosts(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
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
                    return NotFound(BaseResponse<object>.FailureResult("User not found"));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var currentUserId = userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid) ? uid : 0;

                var allPosts = await _unitOfWork.Posts.GetAllWithIncludesAsync(
                    "SubCategory", 
                    "SubCategory.Category", 
                    "Creator", 
                    "Creator.UserRoles", 
                    "Creator.UserRoles.Role",
                    "Owner", 
                    "Owner.UserRoles", 
                    "Owner.UserRoles.Role",
                    "PostImages", 
                    "Photos");
                var posts = allPosts.Where(p => p.CreatorId == id).ToList();
                var totalCount = posts.Count;

                var pagedPosts = posts
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var postDtos = _mapper.Map<List<PostDto>>(pagedPosts);

                return Ok(BaseResponse<object>.SuccessResult(new
                {
                    Posts = postDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }, "User posts retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error retrieving user posts: {ex.Message}"));
            }
        }

    }
}