using AutoMapper;
using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Post;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using Swashbuckle.AspNetCore.Annotations;
using LostAndFound.Api.Services.Interfaces;

namespace LostAndFound.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PostsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly INotificationHubService _notificationHubService;

        /// <summary>
        /// Initializes a new instance of the PostsController.
        /// </summary>
        /// <param name="unitOfWork">Unit of work for database operations</param>
        /// <param name="mapper">AutoMapper for entity-to-DTO mapping</param>
        /// <param name="notificationService">Service for creating notifications</param>
        /// <param name="notificationHubService">Service for real-time notification delivery</param>
        public PostsController(IUnitOfWork unitOfWork, IMapper mapper, INotificationService notificationService, INotificationHubService notificationHubService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
            _notificationHubService = notificationHubService;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Get all posts",
            Description = "Retrieves a paginated list of all posts in the system, ordered by creation date (newest first). Includes post details, creator information, and images. Requires authentication."
        )]
        public async Task<IActionResult> GetAllPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
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
                    "Photos",
                    "Likes",
                    "Comments",
                    "Shares");
                var totalCount = allPosts.Count();

                var pagedPosts = allPosts
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var postDtos = _mapper.Map<List<PostDto>>(pagedPosts, opts => opts.Items["CurrentUserId"] = userId);

                return Ok(BaseResponse<object>.SuccessResult(new
                {
                    Posts = postDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }, "Posts retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error retrieving posts: {ex.Message}"));
            }
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Get post by ID",
            Description = "Retrieves detailed information about a specific post including content, images, creator, and category information. Requires authentication."
        )]
        public async Task<IActionResult> GetPost(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
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
                    "Photos",
                    "Likes",
                    "Comments",
                    "Shares");
                var post = allPosts.FirstOrDefault(p => p.Id == id);
                
                if (post == null)
                {
                    return NotFound(BaseResponse<PostDto>.FailureResult("Post not found"));
                }
                
                var postDto = _mapper.Map<PostDto>(post, opts => opts.Items["CurrentUserId"] = userId);

                return Ok(BaseResponse<PostDto>.SuccessResult(postDto, "Post retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<PostDto>.FailureResult($"Error retrieving post: {ex.Message}"));
            }
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(
            Summary = "Create a new post",
            Description = "Creates a new lost or found post with content, category, location, and optional images. Supports file uploads for post images. Automatically notifies users with matching posts. Requires authentication."
        )]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto createPostDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                {
                    return Unauthorized(BaseResponse<PostDto>.FailureResult("Invalid user token"));
                }

                if (string.IsNullOrWhiteSpace(createPostDto.Content))
                {
                    return BadRequest(BaseResponse<PostDto>.FailureResult("Content is required."));
                }

                if (createPostDto.Reward.HasValue && createPostDto.Reward.Value <= 0)
                {
                    return BadRequest(BaseResponse<PostDto>.FailureResult("Reward must be greater than zero when provided."));
                }

                var subCategory = await _unitOfWork.SubCategories.GetByIdAsync(createPostDto.SubCategoryId);
                if (subCategory == null)
                {
                    return BadRequest(BaseResponse<PostDto>.FailureResult("SubCategory not found."));
                }

                var post = _mapper.Map<Post>(createPostDto);
                post.CreatorId = userId; 
                post.OwnerId = userId;

                if (createPostDto.Reward.HasValue)
                {
                    var (rewardAmount, platformFee) = CalculateRewardBreakdown(createPostDto.Reward.Value);
                    post.RewardAmount = rewardAmount;
                    post.PlatformFeeAmount = platformFee;
                }

                await _unitOfWork.Posts.AddAsync(post);
                await _unitOfWork.SaveChangesAsync();

                if (createPostDto.Photos != null && createPostDto.Photos.Count > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "posts", post.Id.ToString());
                    
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    foreach (var photoFile in createPostDto.Photos)
                    {
                        if (photoFile == null || photoFile.Length == 0)
                            continue;

                        // Validate file type
                        var fileExtension = Path.GetExtension(photoFile.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(fileExtension))
                            continue;

                        // Validate file size (max 10MB)
                        if (photoFile.Length > 10 * 1024 * 1024)
                            continue;

                        // Generate unique filename
                        var fileName = $"{Guid.NewGuid()}{fileExtension}";
                        var filePath = Path.Combine(uploadsPath, fileName);

                        // Save file to local storage
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await photoFile.CopyToAsync(stream);
                        }

                        // Generate URL
                        var photoUrl = $"/uploads/posts/{post.Id}/{fileName}";

                        // Create Photo entity
                        var photo = new Photo
                        {
                            PostId = post.Id,
                            Url = photoUrl,
                            PublicId = fileName,
                            UploadedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.Photos.AddAsync(photo);
                    }

                    await _unitOfWork.SaveChangesAsync();
                }
                
                // Check for matching posts and notify owners
                try
                {
                    var existingPosts = await _unitOfWork.Posts.GetAllAsync();
                    var activePosts = existingPosts.Where(p => p.Status == "Active" && p.Id != post.Id && p.CreatorId != userId).ToList();
                    
                    foreach (var existingPost in activePosts)
                    {
                        var notification = await _notificationService.NotifyMatchingPostAsync(existingPost, post);
                        if (notification != null)
                        {
                            await _notificationHubService.SendNotificationAsync(notification);
                        }
                    }
                }
                catch
                {
                    // Log error but don't fail the post creation
                }
                
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
                    "Photos",
                    "Likes",
                    "Comments",
                    "Shares");
                var postWithPhotos = allPosts.FirstOrDefault(p => p.Id == post.Id);
                var postDto = _mapper.Map<PostDto>(postWithPhotos, opts => opts.Items["CurrentUserId"] = userId);
                
                return CreatedAtAction(nameof(GetPost), new { id = post.Id }, BaseResponse<PostDto>.SuccessResult(postDto, "Post created successfully"));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" Inner exception: {ex.InnerException.Message}";
                }
                return StatusCode(500, BaseResponse<PostDto>.FailureResult($"Error creating post: {errorMessage}"));
            }
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(
            Summary = "Update post",
            Description = "Updates an existing post. Only the post creator or Admin can update posts. Supports updating content, category, location, status, and images. Requires authentication."
        )]
        public async Task<IActionResult> UpdatePost(int id, [FromForm] UpdatePostDto updatePostDto)
        {
            try
            {
                if (id != updatePostDto.Id)
                {
                    return BadRequest(BaseResponse<PostDto>.FailureResult("ID mismatch"));
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                {
                    return Unauthorized(BaseResponse<PostDto>.FailureResult("Invalid user token"));
                }

                var existingPost = await _unitOfWork.Posts.GetByIdAsync(id);
                if (existingPost == null)
                {
                    return NotFound(BaseResponse<PostDto>.FailureResult("Post not found"));
                }

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
                if (existingPost.CreatorId != userId && userRole != "Admin")
                {
                    return StatusCode(403, BaseResponse<PostDto>.FailureResult("You can only update your own posts"));
                }

                if (string.IsNullOrWhiteSpace(updatePostDto.Content))
                {
                    return BadRequest(BaseResponse<PostDto>.FailureResult("Content is required."));
                }

                if (updatePostDto.Reward.HasValue && updatePostDto.Reward.Value <= 0)
                {
                    return BadRequest(BaseResponse<PostDto>.FailureResult("Reward must be greater than zero when provided."));
                }

                existingPost.Content = updatePostDto.Content;
                existingPost.SubCategoryId = updatePostDto.SubCategoryId;
                existingPost.Latitude = updatePostDto.Latitude;
                existingPost.Longitude = updatePostDto.Longitude;
                if (updatePostDto.Address != null)
                {
                    existingPost.Address = updatePostDto.Address;
                }

                if (updatePostDto.Reward.HasValue)
                {
                    var (rewardAmount, platformFee) = CalculateRewardBreakdown(updatePostDto.Reward.Value);
                    existingPost.RewardAmount = rewardAmount;
                    existingPost.PlatformFeeAmount = platformFee;
                }

                if (!string.IsNullOrEmpty(updatePostDto.Status))
                {
                    existingPost.Status = updatePostDto.Status;
                }
                existingPost.UpdatedAt = DateTime.UtcNow;

                if (updatePostDto.PhotoIdsToRemove != null && updatePostDto.PhotoIdsToRemove.Count > 0)
                {
                    var photosToRemove = await _unitOfWork.Photos.FindAsync(p => 
                        updatePostDto.PhotoIdsToRemove.Contains(p.Id) && p.PostId == id);
                    
                    foreach (var photo in photosToRemove)
                    {
                        await _unitOfWork.Photos.DeleteAsync(photo);
                    }
                }

                if (updatePostDto.NewPhotos != null && updatePostDto.NewPhotos.Count > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "posts", id.ToString());
                    
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    foreach (var photoFile in updatePostDto.NewPhotos)
                    {
                        if (photoFile == null || photoFile.Length == 0)
                            continue;

                        var fileExtension = Path.GetExtension(photoFile.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(fileExtension))
                            continue;

                        if (photoFile.Length > 10 * 1024 * 1024)
                            continue;

                        var fileName = $"{Guid.NewGuid()}{fileExtension}";
                        var filePath = Path.Combine(uploadsPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await photoFile.CopyToAsync(stream);
                        }

                        var photoUrl = $"/uploads/posts/{id}/{fileName}";

                        var photo = new Photo
                        {
                            PostId = id,
                            Url = photoUrl,
                            PublicId = fileName,
                            UploadedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.Photos.AddAsync(photo);
                    }
                }
                
                await _unitOfWork.Posts.UpdateAsync(existingPost);
                await _unitOfWork.SaveChangesAsync();
                
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
                    "Photos",
                    "Likes",
                    "Comments",
                    "Shares");
                var updatedPost = allPosts.FirstOrDefault(p => p.Id == id);
                var postDto = _mapper.Map<PostDto>(updatedPost, opts => opts.Items["CurrentUserId"] = userId);
                
                return Ok(BaseResponse<PostDto>.SuccessResult(postDto, "Post updated successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<PostDto>.FailureResult($"Error updating post: {ex.Message}"));
            }
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Delete post",
            Description = "Deletes a post from the system. Only the post creator or Admin can delete posts. Requires authentication."
        )]
        public async Task<IActionResult> DeletePost(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                {
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));
                }

                var post = await _unitOfWork.Posts.GetByIdAsync(id);
                
                if (post == null)
                {
                    return NotFound(BaseResponse<object>.FailureResult("Post not found"));
                }

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
                if (post.CreatorId != userId && userRole != "Admin")
                {
                    return Forbid("You can only delete your own posts");
                }

                await _unitOfWork.Posts.DeleteAsync(post);
                await _unitOfWork.SaveChangesAsync();
                
                return Ok(BaseResponse<object>.SuccessResult(new object(), "Post deleted successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error deleting post: {ex.Message}"));
            }
        }

        [HttpGet("my-posts")]
        [SwaggerOperation(
            Summary = "Get my posts",
            Description = "Retrieves all posts created by the authenticated user with pagination support. Returns posts ordered by creation date (newest first). Requires authentication."
        )]
        public async Task<IActionResult> GetMyPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                {
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));
                }

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
                    "Photos",
                    "Likes",
                    "Comments",
                    "Shares");
                var posts = allPosts.Where(p => p.CreatorId == userId).ToList();
                var totalCount = posts.Count;

                var pagedPosts = posts
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var postDtos = _mapper.Map<List<PostDto>>(pagedPosts, opts => opts.Items["CurrentUserId"] = userId);

                return Ok(BaseResponse<object>.SuccessResult(new
                {
                    Posts = postDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }, "My posts retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error retrieving posts: {ex.Message}"));
            }
        }

        [HttpGet("feed")]
        [SwaggerOperation(
            Summary = "Get feed",
            Description = "Retrieves a paginated feed of active posts, ordered by creation date (newest first). Only includes posts with 'Active' status. Requires authentication."
        )]
        public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
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
                    "Photos",
                    "Likes",
                    "Comments",
                    "Shares");

                // Filter only active posts
                var posts = allPosts.Where(p => p.Status == "Active").ToList();

                var totalCount = posts.Count;

                var pagedPosts = posts
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var postDtos = _mapper.Map<List<PostDto>>(pagedPosts, opts => opts.Items["CurrentUserId"] = userId);

                return Ok(BaseResponse<object>.SuccessResult(new
                {
                    Posts = postDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }, "Feed retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error retrieving feed: {ex.Message}"));
            }
        }

        [HttpPut("{id}/status")]
        [SwaggerOperation(
            Summary = "Update post status",
            Description = "Updates the status of a post (Active, Resolved, Closed). Only the post creator or Admin can update post status. Requires authentication."
        )]
        public async Task<IActionResult> UpdatePostStatus(int id, [FromBody] UpdatePostStatusDto updateStatusDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                {
                    return Unauthorized(BaseResponse<PostDto>.FailureResult("Invalid user token"));
                }

                var post = await _unitOfWork.Posts.GetByIdAsync(id);
                if (post == null)
                {
                    return NotFound(BaseResponse<PostDto>.FailureResult("Post not found"));
                }

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
                if (post.CreatorId != userId && userRole != "Admin")
                {
                    return Forbid("You can only update status of your own posts");
                }

                var validStatuses = new[] { "Active", "Resolved", "Closed" };
                if (!validStatuses.Contains(updateStatusDto.Status))
                {
                    return BadRequest(BaseResponse<PostDto>.FailureResult("Invalid status. Allowed: Active, Resolved, Closed"));
                }

                post.Status = updateStatusDto.Status;
                if (updateStatusDto.Status == "Resolved" || updateStatusDto.Status == "Closed")
                {
                    post.ResolvedAt = DateTime.UtcNow;
                    post.ResolvedByUserId = userId;
                }
                post.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Posts.UpdateAsync(post);
                await _unitOfWork.SaveChangesAsync();

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
                    "Photos",
                    "Likes",
                    "Comments",
                    "Shares");
                var updatedPost = allPosts.FirstOrDefault(p => p.Id == id);
                var postDto = _mapper.Map<PostDto>(updatedPost, opts => opts.Items["CurrentUserId"] = userId);
                
                return Ok(BaseResponse<PostDto>.SuccessResult(postDto, "Post status updated successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<PostDto>.FailureResult($"Error updating post status: {ex.Message}"));
            }
        }

        private static (decimal rewardAmount, decimal platformFee) CalculateRewardBreakdown(decimal reward)
        {
            var normalizedReward = Math.Round(reward, 2, MidpointRounding.AwayFromZero);
            var platformFee = Math.Round(normalizedReward * 0.20m, 2, MidpointRounding.AwayFromZero);
            return (normalizedReward, platformFee);
        }
    }
}



