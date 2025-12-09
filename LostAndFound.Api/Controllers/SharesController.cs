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
    public class SharesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public SharesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Share a post
        /// </summary>
        [HttpPost]
        [SwaggerOperation(Summary = "Share a post", Description = "Shares the specified post. Requires authentication.")]
        public async Task<IActionResult> SharePost(int postId)
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

                var share = new Share
                {
                    PostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Shares.AddAsync(share);
                await _unitOfWork.SaveChangesAsync();

                return Ok(BaseResponse<object>.SuccessResult(new { shareId = share.Id }, "Post shared successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error sharing post: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get all shares for a post
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Get post shares", Description = "Retrieves all shares for the specified post. Requires authentication.")]
        public async Task<IActionResult> GetPostShares(int postId)
        {
            try
            {
                var shares = await _unitOfWork.Shares.GetQueryable()
                    .Include(s => s.User)
                    .Where(s => s.PostId == postId)
                    .Select(s => new ShareDto
                    {
                        Id = s.Id,
                        PostId = s.PostId,
                        UserId = s.UserId,
                        UserName = s.User.FullName,
                        CreatedAt = s.CreatedAt
                    })
                    .ToListAsync();

                return Ok(BaseResponse<List<ShareDto>>.SuccessResult(shares, "Shares retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<List<ShareDto>>.FailureResult($"Error retrieving shares: {ex.Message}"));
            }
        }

        /// <summary>
        /// Un-share a post
        /// </summary>
        [HttpDelete]
        [SwaggerOperation(Summary = "Un-share a post", Description = "Removes the share from the specified post. Requires authentication.")]
        public async Task<IActionResult> DeleteShare(int postId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                var share = await _unitOfWork.Shares.GetQueryable()
                    .FirstOrDefaultAsync(s => s.PostId == postId && s.UserId == userId);

                if (share == null)
                    return NotFound(BaseResponse<object>.FailureResult("Share not found"));

                await _unitOfWork.Shares.DeleteAsync(share);
                await _unitOfWork.SaveChangesAsync();

                return Ok(BaseResponse<object>.SuccessResult(null, "Post un-shared successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error un-sharing post: {ex.Message}"));
            }
        }
    }
}
