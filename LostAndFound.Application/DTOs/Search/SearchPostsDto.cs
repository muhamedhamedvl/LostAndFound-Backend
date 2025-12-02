namespace LostAndFound.Application.DTOs.Search
{
    public class SearchPostsDto
    {
        public string? Content { get; set; }
        public int? CategoryId { get; set; } // Filter by main category
        public int? SubCategoryId { get; set; } // Filter by subcategory
        public string? Address { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}

