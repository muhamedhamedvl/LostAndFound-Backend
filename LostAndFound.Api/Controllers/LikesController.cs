using AutoMapper;
using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Social;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace LostAndFound.Api.Controllers
{
    [Route("api/posts/{postId}/[controller]")]
    [ApiController]
    [Authorize]
    public class LikesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public LikesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        /// <summary>
        /// Like a post
        /// </summary>
        [HttpPost]
        [SwaggerOperation(Summary = "Like a post", Description = "Adds a like to the specified post. Requires authentication.")]
        public async Task<IActionResult> LikePost(int postId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                // Check if post exists
                var post = await _unitOfWork.Posts.GetByIdAsync(postId);
                if (post == null)
                    return NotFound(BaseResponse<object>.FailureResult("Post not found"));

                // Check if already liked
                var existingLike = await _unitOfWork.Likes.GetQueryable()
                    .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

                if (existingLike != null)
                    return BadRequest(BaseResponse<object>.FailureResult("You have already liked this post"));

                // Create like
                var like = new Like
                {
                    PostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Likes.AddAsync(like);
                await _unitOfWork.SaveChangesAsync();

                return Ok(BaseResponse<object>.SuccessResult(new { likeId = like.Id }, "Post liked successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error liking post: {ex.Message}"));
            }
        }

        /// <summary>
        /// Unlike a post
        /// </summary>
        [HttpDelete]
        [SwaggerOperation(Summary = "Unlike a post", Description = "Removes the like from the specified post. Requires authentication.")]
        public async Task<IActionResult> UnlikePost(int postId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                var like = await _unitOfWork.Likes.GetQueryable()
                    .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

                if (like == null)
                    return NotFound(BaseResponse<object>.FailureResult("Like not found"));

                await _unitOfWork.Likes.DeleteAsync(like);
                await _unitOfWork.SaveChangesAsync();

                return Ok(BaseResponse<object>.SuccessResult(null, "Post unliked successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error unliking post: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get all likes for a post
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Get post likes", Description = "Retrieves all likes for the specified post. Requires authentication.")]
        public async Task<IActionResult> GetPostLikes(int postId)
        {
            try
            {
                var likes = await _unitOfWork.Likes.GetQueryable()
                    .Include(l => l.User)
                    .Where(l => l.PostId == postId)
                    .Select(l => new LikeDto
                    {
                        Id = l.Id,
                        PostId = l.PostId,
                        UserId = l.UserId,
                        UserName = l.User.FullName,
                        CreatedAt = l.CreatedAt
                    })
                    .ToListAsync();

                return Ok(BaseResponse<List<LikeDto>>.SuccessResult(likes, "Likes retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<List<LikeDto>>.FailureResult($"Error retrieving likes: {ex.Message}"));
            }
        }
    }
}
