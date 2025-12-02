using AutoMapper;
using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Category;
using LostAndFound.Application.DTOs.Post;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
namespace LostAndFound.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CategoriesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Get all categories",
            Description = "Retrieves a flat list of all categories without nested SubCategories. Returns only basic category information (Id, Name, Description, SubCategoryCount) for lightweight responses. Requires authentication."
        )]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                var categories = await _unitOfWork.Categories.GetAllWithIncludesAsync("SubCategories");
                var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);
                return Ok(BaseResponse<IEnumerable<CategoryDto>>.SuccessResult(categoryDtos, "Categories retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<IEnumerable<CategoryDto>>.FailureResult($"Error retrieving categories: {ex.Message}"));
            }
        }

        [HttpGet("tree")]
        [SwaggerOperation(
            Summary = "Get all categories along with their subcategories",
            Description = "Retrieves the full hierarchical category tree with nested SubCategories. Returns complete parent-child relationships for building tree structures. Requires authentication."
        )]
        public async Task<IActionResult> GetCategoryTree()
        {
            try
            {
                var postCounts = await _unitOfWork.Posts.GetQueryable()
                    .GroupBy(p => p.SubCategoryId)
                    .Select(g => new { SubCategoryId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.SubCategoryId, x => x.Count);

                var categories = await _unitOfWork.Categories.GetAllWithIncludesAsync("SubCategories");
                
                var categoryTree = _mapper.Map<List<CategoryTreeDto>>(categories);
                
                foreach (var category in categoryTree)
                {
                    foreach (var subCategory in category.SubCategories)
                    {
                        subCategory.PostCount = postCounts.ContainsKey(subCategory.Id) ? postCounts[subCategory.Id] : 0;
                    }
                }
                
                return Ok(BaseResponse<List<CategoryTreeDto>>.SuccessResult(categoryTree, "Category tree retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<List<CategoryTreeDto>>.FailureResult($"Error retrieving category tree: {ex.Message}"));
            }
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Get category by ID",
            Description = "Retrieves a single category by its ID with basic information. Requires authentication."
        )]
        public async Task<IActionResult> GetCategory(int id)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetAllWithIncludesAsync("SubCategories");
                var foundCategory = category.FirstOrDefault(c => c.Id == id);
                
                if (foundCategory == null)
                {
                    return NotFound(BaseResponse<CategoryDto>.FailureResult("Category not found"));
                }
                
                var categoryDto = _mapper.Map<CategoryDto>(foundCategory);
                return Ok(BaseResponse<CategoryDto>.SuccessResult(categoryDto, "Category retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<CategoryDto>.FailureResult($"Error retrieving category: {ex.Message}"));
            }
        }

        [HttpGet("{id}/posts")]
        [SwaggerOperation(
            Summary = "Get posts by category",
            Description = "Retrieves all posts that belong to subcategories under the specified category. Requires authentication."
        )]
        public async Task<IActionResult> GetCategoryPosts(int id)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetAllWithIncludesAsync("SubCategories");
                var foundCategory = category.FirstOrDefault(c => c.Id == id);
                
                if (foundCategory == null)
                {
                    return NotFound(BaseResponse<object>.FailureResult("Category not found"));
                }
                var subCategoryIds = foundCategory.SubCategories.Select(sc => sc.Id).ToList();
               
                var allPosts = await _unitOfWork.Posts.GetAllWithIncludesAsync("SubCategory", "SubCategory.Category", "Creator", "PostImages", "Owner");
                var posts = allPosts.Where(p => subCategoryIds.Contains(p.SubCategoryId)).ToList();
                
                var postDtos = _mapper.Map<IEnumerable<PostDto>>(posts);
                return Ok(BaseResponse<IEnumerable<PostDto>>.SuccessResult(postDtos, $"Posts for category '{foundCategory.Name}' retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error retrieving category posts: {ex.Message}"));
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Create a new category",
            Description = "Creates a new category in the system. This endpoint is restricted to Admin users only. Requires Admin role."
        )]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto createCategoryDto)
        {
            try
            {
                var category = _mapper.Map<Category>(createCategoryDto);
                await _unitOfWork.Categories.AddAsync(category);
                await _unitOfWork.SaveChangesAsync();
               
                var createdCategory = await _unitOfWork.Categories.GetAllWithIncludesAsync("SubCategories");
                var foundCategory = createdCategory.FirstOrDefault(c => c.Id == category.Id);
                var categoryDto = _mapper.Map<CategoryDto>(foundCategory ?? category);
                
                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, BaseResponse<CategoryDto>.SuccessResult(categoryDto, "Category created successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<CategoryDto>.FailureResult($"Error creating category: {ex.Message}"));
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Update category",
            Description = "Updates an existing category's information. This endpoint is restricted to Admin users only. Requires Admin role."
        )]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto updateCategoryDto)
        {
            try
            {
                if (id != updateCategoryDto.Id)
                {
                    return BadRequest(BaseResponse<CategoryDto>.FailureResult("ID mismatch"));
                }

                var existingCategory = await _unitOfWork.Categories.GetByIdAsync(id);
                if (existingCategory == null)
                {
                    return NotFound(BaseResponse<CategoryDto>.FailureResult("Category not found"));
                }

                _mapper.Map(updateCategoryDto, existingCategory);
                existingCategory.UpdatedAt = DateTime.UtcNow;
                
                await _unitOfWork.Categories.UpdateAsync(existingCategory);
                await _unitOfWork.SaveChangesAsync();
                
                var updatedCategory = await _unitOfWork.Categories.GetAllWithIncludesAsync("SubCategories");
                var foundCategory = updatedCategory.FirstOrDefault(c => c.Id == id);
                var categoryDto = _mapper.Map<CategoryDto>(foundCategory ?? existingCategory);
                
                return Ok(BaseResponse<CategoryDto>.SuccessResult(categoryDto, "Category updated successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<CategoryDto>.FailureResult($"Error updating category: {ex.Message}"));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Delete category",
            Description = "Deletes a category from the system. Categories with existing subcategories cannot be deleted. This endpoint is restricted to Admin users only. Requires Admin role."
        )]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(id);
                
                if (category == null)
                {
                    return NotFound(BaseResponse<object>.FailureResult("Category not found"));
                }

                var categoryWithSubs = await _unitOfWork.Categories.GetAllWithIncludesAsync("SubCategories");
                var foundCategory = categoryWithSubs.FirstOrDefault(c => c.Id == id);
                
                if (foundCategory != null && foundCategory.SubCategories.Any())
                {
                    return BadRequest(BaseResponse<object>.FailureResult("Cannot delete category that has subcategories. Please delete or reassign subcategories first."));
                }

                await _unitOfWork.Categories.DeleteAsync(category);
                await _unitOfWork.SaveChangesAsync();
                
                return Ok(BaseResponse<object>.SuccessResult(new object(), "Category deleted successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error deleting category: {ex.Message}"));
            }
        }
    }
}
//code writer : muhamed hamed
