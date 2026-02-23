namespace LostAndFound.Application.DTOs.Category
{
    public class CategoryMappingDto
    {
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public int SubCategoryId { get; set; }
    }
}
