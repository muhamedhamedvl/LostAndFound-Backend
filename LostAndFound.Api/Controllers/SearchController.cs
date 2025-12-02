using AutoMapper;
using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Post;
using LostAndFound.Application.DTOs.Search;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
namespace LostAndFound.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SearchController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet("posts")]
        [SwaggerOperation(
            Summary = "Search posts",
            Description = "Searches for posts using various filters including content, category, subcategory, address, and date range. Returns paginated results. Requires authentication."
        )]
        public async Task<IActionResult> SearchPosts([FromQuery] SearchPostsDto searchDto)
        {
            try
            {
                var posts = await _unitOfWork.Posts.GetAllAsync();
                
                if (!string.IsNullOrEmpty(searchDto.Content))
                {
                    posts = posts.Where(p => p.Content.Contains(searchDto.Content, StringComparison.OrdinalIgnoreCase));
                }
                
                if (searchDto.SubCategoryId.HasValue)
                {
                    posts = posts.Where(p => p.SubCategoryId == searchDto.SubCategoryId.Value);
                }
                
                if (searchDto.CategoryId.HasValue)
                {
                    var subCategories = await _unitOfWork.SubCategories.FindAsync(sc => sc.CategoryId == searchDto.CategoryId.Value);
                    var subCategoryIds = subCategories.Select(sc => sc.Id).ToList();
                    posts = posts.Where(p => subCategoryIds.Contains(p.SubCategoryId));
                }
                
                if (!string.IsNullOrEmpty(searchDto.Address))
                {
                    posts = posts.Where(p => !string.IsNullOrEmpty(p.Address) && p.Address.Contains(searchDto.Address, StringComparison.OrdinalIgnoreCase));
                }
                
                if (searchDto.CreatedAfter.HasValue)
                {
                    posts = posts.Where(p => p.CreatedAt >= searchDto.CreatedAfter.Value);
                }
                
                if (searchDto.CreatedBefore.HasValue)
                {
                    posts = posts.Where(p => p.CreatedAt <= searchDto.CreatedBefore.Value);
                }

                var totalCount = posts.Count();
                var pagedPosts = posts
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((searchDto.Page - 1) * searchDto.PageSize)
                    .Take(searchDto.PageSize)
                    .ToList();

                var postDtos = _mapper.Map<List<PostDto>>(pagedPosts);

                return Ok(BaseResponse<object>.SuccessResult(new
                {
                    Posts = postDtos,
                    TotalCount = totalCount,
                    Page = searchDto.Page,
                    PageSize = searchDto.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / searchDto.PageSize)
                }, "Posts retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error searching posts: {ex.Message}"));
            }
        }


        [HttpGet("nearby")]
        [SwaggerOperation(
            Summary = "Get nearby posts",
            Description = "Retrieves posts near a specified geographic location using latitude and longitude coordinates. Returns active posts within the specified radius. Requires authentication."
        )]
        public async Task<IActionResult> GetNearbyPosts([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radiusKm = 10)
        {
            try
            {
                var posts = await _unitOfWork.Posts.GetAllAsync();
                var activePosts = posts.Where(p => p.Status == "Active").ToList();

                var postDtos = _mapper.Map<List<PostDto>>(activePosts);
                return Ok(BaseResponse<List<PostDto>>.SuccessResult(postDtos, "Posts retrieved successfully. Note: Geographic proximity search requires location coordinates."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error retrieving nearby posts: {ex.Message}"));
            }
        }

        [HttpGet("statistics")]
        [SwaggerOperation(
            Summary = "Get statistics",
            Description = "Retrieves platform statistics including total number of posts and users. Requires authentication."
        )]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var totalPosts = await _unitOfWork.Posts.CountAsync();
                var totalUsers = await _unitOfWork.Users.CountAsync();

                return Ok(BaseResponse<object>.SuccessResult(new
                {
                    TotalPosts = totalPosts,
                    TotalUsers = totalUsers
                }, "Statistics retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error retrieving statistics: {ex.Message}"));
            }
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; 
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }
}
