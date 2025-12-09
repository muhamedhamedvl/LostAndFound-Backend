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
    public class CommentsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CommentsController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        /// <summary>
        /// Add a comment to a post
        /// </summary>
        [HttpPost]
        [SwaggerOperation(Summary = "Add comment", Description = "Adds a comment to the specified post. Requires authentication.")]
        public async Task<IActionResult> AddComment(int postId, [FromBody] CreateCommentDto createCommentDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                if (string.IsNullOrWhiteSpace(createCommentDto.Content))
                    return BadRequest(BaseResponse<object>.FailureResult("Comment content is required"));

                // Check if post exists
                var post = await _unitOfWork.Posts.GetByIdAsync(postId);
                if (post == null)
                    return NotFound(BaseResponse<object>.FailureResult("Post not found"));

                var comment = new Comment
                {
                    PostId = postId,
                    UserId = userId,
                    Content = createCommentDto.Content,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Comments.AddAsync(comment);
                await _unitOfWork.SaveChangesAsync();

                // Load user info
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                
                var commentDto = new CommentDto
                {
                    Id = comment.Id,
                    PostId = comment.PostId,
                    UserId = comment.UserId,
                    UserName = user?.FullName ?? "Unknown",
                    Content = comment.Content,
                    CreatedAt = comment.CreatedAt
                };

                return Ok(BaseResponse<CommentDto>.SuccessResult(commentDto, "Comment added successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error adding comment: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get all comments for a post
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Get comments", Description = "Retrieves all comments for the specified post. Requires authentication.")]
        public async Task<IActionResult> GetComments(int postId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var comments = await _unitOfWork.Comments.GetQueryable()
                    .Include(c => c.User)
                    .Where(c => c.PostId == postId)
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new CommentDto
                    {
                        Id = c.Id,
                        PostId = c.PostId,
                        UserId = c.UserId,
                        UserName = c.User.FullName,
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt
                    })
                    .ToListAsync();

                var totalCount = await _unitOfWork.Comments.GetQueryable()
                    .Where(c => c.PostId == postId)
                    .CountAsync();

                return Ok(BaseResponse<object>.SuccessResult(new
                {
                    comments,
                    totalCount,
                    page,
                    pageSize
                }, "Comments retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<List<CommentDto>>.FailureResult($"Error retrieving comments: {ex.Message}"));
            }
        }

        /// <summary>
        /// Delete a comment
        /// </summary>
        [HttpDelete("{commentId}")]
        [SwaggerOperation(Summary = "Delete comment", Description = "Deletes a comment. Users can only delete their own comments. Admins can delete any comment. Requires authentication.")]
        public async Task<IActionResult> DeleteComment(int postId, int commentId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Unauthorized(BaseResponse<object>.FailureResult("Invalid user token"));

                var comment = await _unitOfWork.Comments.GetQueryable()
                    .FirstOrDefaultAsync(c => c.Id == commentId && c.PostId == postId);

                if (comment == null)
                    return NotFound(BaseResponse<object>.FailureResult("Comment not found"));

                // Check if user owns the comment or is an admin
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
                if (comment.UserId != userId && userRole != "Admin")
                    return StatusCode(403, BaseResponse<object>.FailureResult("You can only delete your own comments"));

                await _unitOfWork.Comments.DeleteAsync(comment);
                await _unitOfWork.SaveChangesAsync();

                return Ok(BaseResponse<object>.SuccessResult(null, "Comment deleted successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error deleting comment: {ex.Message}"));
            }
        }
    }
}
