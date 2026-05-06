using System;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LostAndFound.Application.DTOs.Report
{
    public class ReportFilterDto
    {
        /// <summary>
        /// Flag set by server only; client cannot override via query string.
        /// </summary>
        [BindNever]
        [JsonIgnore]
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
