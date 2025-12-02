using AutoMapper;
using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Category;
using LostAndFound.Application.DTOs.Post;
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
    public class SubCategoriesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SubCategoriesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Get all subcategories",
            Description = "Retrieves a list of all subcategories with their associated category information. Requires authentication."
        )]
        public async Task<IActionResult> GetAllSubCategories()
        {
            try
            {
                var subCategories = await _unitOfWork.SubCategories.GetAllWithIncludesAsync("Category");
                var subCategoryDtos = _mapper.Map<IEnumerable<SubCategoryDto>>(subCategories);
                return Ok(BaseResponse<IEnumerable<SubCategoryDto>>.SuccessResult(subCategoryDtos, "SubCategories retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<IEnumerable<SubCategoryDto>>.FailureResult($"Error retrieving subCategories: {ex.Message}"));
            }
        }

        [HttpGet("category/{categoryId}")]
        [SwaggerOperation(
            Summary = "Get subcategories by category",
            Description = "Retrieves all subcategories that belong to a specific category. Requires authentication."
        )]
        public async Task<IActionResult> GetSubCategoriesByCategory(int categoryId)
        {
            try
            {
                var subCategories = await _unitOfWork.SubCategories.GetAllWithIncludesAsync("Category");
                var filtered = subCategories.Where(sc => sc.CategoryId == categoryId);
                var subCategoryDtos = _mapper.Map<IEnumerable<SubCategoryDto>>(filtered);
                return Ok(BaseResponse<IEnumerable<SubCategoryDto>>.SuccessResult(subCategoryDtos, "SubCategories retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<IEnumerable<SubCategoryDto>>.FailureResult($"Error retrieving subCategories: {ex.Message}"));
            }
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Get subcategory by ID",
            Description = "Retrieves a single subcategory by its ID with associated category information. Requires authentication."
        )]
        public async Task<IActionResult> GetSubCategory(int id)
        {
            try
            {
                var subCategory = await _unitOfWork.SubCategories.GetAllWithIncludesAsync("Category");
                var found = subCategory.FirstOrDefault(sc => sc.Id == id);
                
                if (found == null)
                {
                    return NotFound(BaseResponse<SubCategoryDto>.FailureResult("SubCategory not found"));
                }
                
                var subCategoryDto = _mapper.Map<SubCategoryDto>(found);
                return Ok(BaseResponse<SubCategoryDto>.SuccessResult(subCategoryDto, "SubCategory retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<SubCategoryDto>.FailureResult($"Error retrieving subCategory: {ex.Message}"));
            }
        }

        [HttpGet("{id}/posts")]
        [SwaggerOperation(
            Summary = "Get posts by subcategory",
            Description = "Retrieves all posts that belong to the specified subcategory. Requires authentication."
        )]
        public async Task<IActionResult> GetSubCategoryPosts(int id)
        {
            try
            {
                var subCategory = await _unitOfWork.SubCategories.GetByIdAsync(id);
                
                if (subCategory == null)
                {
                    return NotFound(BaseResponse<object>.FailureResult("SubCategory not found"));
                }

                var posts = await _unitOfWork.Posts.GetAllWithIncludesAsync("SubCategory", "SubCategory.Category", "Creator", "PostImages", "Owner");
                var filteredPosts = posts.Where(p => p.SubCategoryId == id).ToList();
                
                var postDtos = _mapper.Map<IEnumerable<PostDto>>(filteredPosts);
                return Ok(BaseResponse<IEnumerable<PostDto>>.SuccessResult(postDtos, $"Posts for subcategory '{subCategory.Name}' retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error retrieving subcategory posts: {ex.Message}"));
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Create a new subcategory",
            Description = "Creates a new subcategory under a specified category. This endpoint is restricted to Admin users only. Requires Admin role."
        )]
        public async Task<IActionResult> CreateSubCategory([FromBody] CreateSubCategoryDto createSubCategoryDto)
        {
            try
            {
                // Validate that the category exists
                var category = await _unitOfWork.Categories.GetByIdAsync(createSubCategoryDto.CategoryId);
                if (category == null)
                {
                    return BadRequest(BaseResponse<SubCategoryDto>.FailureResult($"Category with ID {createSubCategoryDto.CategoryId} not found."));
                }

                var subCategory = _mapper.Map<SubCategory>(createSubCategoryDto);
                await _unitOfWork.SubCategories.AddAsync(subCategory);
                await _unitOfWork.SaveChangesAsync();
                
                // Reload with category
                var created = await _unitOfWork.SubCategories.GetAllWithIncludesAsync("Category");
                var found = created.FirstOrDefault(sc => sc.Id == subCategory.Id);
                var subCategoryDto = _mapper.Map<SubCategoryDto>(found ?? subCategory);
                
                return CreatedAtAction(nameof(GetSubCategory), new { id = subCategory.Id }, BaseResponse<SubCategoryDto>.SuccessResult(subCategoryDto, "SubCategory created successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<SubCategoryDto>.FailureResult($"Error creating subCategory: {ex.Message}"));
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Update subcategory",
            Description = "Updates an existing subcategory's information. This endpoint is restricted to Admin users only. Requires Admin role."
        )]
        public async Task<IActionResult> UpdateSubCategory(int id, [FromBody] UpdateSubCategoryDto updateSubCategoryDto)
        {
            try
            {
                if (id != updateSubCategoryDto.Id)
                {
                    return BadRequest(BaseResponse<SubCategoryDto>.FailureResult("ID mismatch"));
                }

                var existingSubCategory = await _unitOfWork.SubCategories.GetByIdAsync(id);
                if (existingSubCategory == null)
                {
                    return NotFound(BaseResponse<SubCategoryDto>.FailureResult("SubCategory not found"));
                }

                // Validate that the category exists
                var category = await _unitOfWork.Categories.GetByIdAsync(updateSubCategoryDto.CategoryId);
                if (category == null)
                {
                    return BadRequest(BaseResponse<SubCategoryDto>.FailureResult($"Category with ID {updateSubCategoryDto.CategoryId} not found."));
                }

                _mapper.Map(updateSubCategoryDto, existingSubCategory);
                existingSubCategory.UpdatedAt = DateTime.UtcNow;
                
                await _unitOfWork.SubCategories.UpdateAsync(existingSubCategory);
                await _unitOfWork.SaveChangesAsync();
                
                // Reload with category
                var updated = await _unitOfWork.SubCategories.GetAllWithIncludesAsync("Category");
                var found = updated.FirstOrDefault(sc => sc.Id == id);
                var subCategoryDto = _mapper.Map<SubCategoryDto>(found ?? existingSubCategory);
                
                return Ok(BaseResponse<SubCategoryDto>.SuccessResult(subCategoryDto, "SubCategory updated successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<SubCategoryDto>.FailureResult($"Error updating subCategory: {ex.Message}"));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Delete subcategory",
            Description = "Deletes a subcategory from the system. Subcategories with existing posts cannot be deleted. This endpoint is restricted to Admin users only. Requires Admin role."
        )]
        public async Task<IActionResult> DeleteSubCategory(int id)
        {
            try
            {
                var subCategory = await _unitOfWork.SubCategories.GetByIdAsync(id);
                
                if (subCategory == null)
                {
                    return NotFound(BaseResponse<object>.FailureResult("SubCategory not found"));
                }

                // Check if subcategory has posts
                var posts = await _unitOfWork.Posts.FindAsync(p => p.SubCategoryId == id);
                if (posts.Any())
                {
                    return BadRequest(BaseResponse<object>.FailureResult("Cannot delete subcategory that has posts. Please delete or reassign posts first."));
                }

                await _unitOfWork.SubCategories.DeleteAsync(subCategory);
                await _unitOfWork.SaveChangesAsync();
                
                return Ok(BaseResponse<object>.SuccessResult(new object(), "SubCategory deleted successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Error deleting subCategory: {ex.Message}"));
            }
        }
    }
}

