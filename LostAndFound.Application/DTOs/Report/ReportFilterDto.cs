using System;

namespace LostAndFound.Application.DTOs.Report
{
    public class ReportFilterDto
    {
        /// <summary>
        /// When true (public view), restricts results to Approved/Matched/Closed when no explicit Status filter is provided.
        /// Set by server only; never bind from client query for security.
        /// </summary>
        public bool ForPublicView { get; set; }

        public string? Type { get; set; }
        public string? Status { get; set; }
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
