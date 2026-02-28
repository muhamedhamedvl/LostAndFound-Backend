using AutoMapper;
using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Category;
using LostAndFound.Application.DTOs.Report;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
namespace LostAndFound.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("api")]
    public class CategoriesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CategoriesController> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
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
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, BaseResponse<IEnumerable<CategoryDto>>.FailureResult("An unexpected error occurred."));
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
                var reportCounts = await _unitOfWork.Reports.GetQueryable()
                    .GroupBy(r => r.SubCategoryId)
                    .Select(g => new { SubCategoryId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.SubCategoryId, x => x.Count);

                var categories = await _unitOfWork.Categories.GetAllWithIncludesAsync("SubCategories");
                
                var categoryTree = _mapper.Map<List<CategoryTreeDto>>(categories);
                
                foreach (var category in categoryTree)
                {
                    foreach (var subCategory in category.SubCategories)
                    {
                        subCategory.ReportCount = reportCounts.ContainsKey(subCategory.Id) ? reportCounts[subCategory.Id] : 0;
                    }
                }
                
                return Ok(BaseResponse<List<CategoryTreeDto>>.SuccessResult(categoryTree, "Category tree retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category tree");
                return StatusCode(500, BaseResponse<List<CategoryTreeDto>>.FailureResult("An unexpected error occurred."));
            }
        }

        [HttpGet("mapping")]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "Get category-subcategory mapping for mobile",
            Description = "Returns a flat list of all category-subcategory-id mappings for mobile app dropdowns. Public endpoint - no authentication required."
        )]
        public async Task<IActionResult> GetCategoryMapping()
        {
            try
            {
                var categories = await _unitOfWork.Categories.GetAllWithIncludesAsync("SubCategories");
                
                var mappings = categories
                    .SelectMany(c => c.SubCategories.Select(sc => new CategoryMappingDto
                    {
                        Category = c.Name,
                        SubCategory = sc.Name,
                        SubCategoryId = sc.Id
                    }))
                    .OrderBy(m => m.Category)
                    .ThenBy(m => m.SubCategory)
                    .ToList();
                
                return Ok(BaseResponse<List<CategoryMappingDto>>.SuccessResult(mappings, "Category mapping retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category mapping");
                return StatusCode(500, BaseResponse<List<CategoryMappingDto>>.FailureResult("An unexpected error occurred."));
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
                _logger.LogError(ex, "Error retrieving category {Id}", id);
                return StatusCode(500, BaseResponse<CategoryDto>.FailureResult("An unexpected error occurred."));
            }
        }

        [HttpGet("{id}/reports")]
        [SwaggerOperation(
            Summary = "Get reports by category",
            Description = "Retrieves all reports that belong to subcategories under the specified category. Requires authentication."
        )]
        public async Task<IActionResult> GetCategoryReports(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var category = await _unitOfWork.Categories.GetAllWithIncludesAsync("SubCategories");
                var foundCategory = category.FirstOrDefault(c => c.Id == id);
                
                if (foundCategory == null)
                {
                    return NotFound(BaseResponse<object>.FailureResult("Category not found"));
                }
                var subCategoryIds = foundCategory.SubCategories.Select(sc => sc.Id).ToList();
               
                var allReports = await _unitOfWork.Reports.GetAllWithIncludesAsync("SubCategory", "SubCategory.Category", "CreatedBy", "Images");
                var reports = allReports.Where(r => subCategoryIds.Contains(r.SubCategoryId)).ToList();
                var totalCount = reports.Count;
                var paginatedReports = reports.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                
                var reportDtos = _mapper.Map<IEnumerable<ReportDto>>(paginatedReports);
                return Ok(BaseResponse<object>.SuccessResult(new
                {
                    reports = reportDtos,
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }, $"Reports for category '{foundCategory.Name}' retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category reports for {Id}", id);
                return StatusCode(500, BaseResponse<object>.FailureResult("An unexpected error occurred."));
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
                _logger.LogError(ex, "Error creating category");
                return StatusCode(500, BaseResponse<CategoryDto>.FailureResult("An unexpected error occurred."));
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
                _logger.LogError(ex, "Error updating category {Id}", id);
                return StatusCode(500, BaseResponse<CategoryDto>.FailureResult("An unexpected error occurred."));
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
                _logger.LogError(ex, "Error deleting category {Id}", id);
                return StatusCode(500, BaseResponse<object>.FailureResult("An unexpected error occurred."));
            }
        }
    }
}
//code writer : muhamed hamed
