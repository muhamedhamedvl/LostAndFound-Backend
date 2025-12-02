using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostAndFound.Application.DTOs.Item
{
    public class UpdateItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public string? Status { get; set; } // "Active", "Resolved", "Closed"
        public int? LocationId { get; set; }
    }
}
