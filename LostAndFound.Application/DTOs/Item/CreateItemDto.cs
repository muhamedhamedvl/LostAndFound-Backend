using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostAndFound.Application.DTOs.Item
{
    public class CreateItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; } // Can be main or sub category
        public string ReportType { get; set; } = string.Empty; // "Lost" or "Found"
        public int CreatorId { get; set; }
        public int? LocationId { get; set; }
        public List<string> PhotoUrls { get; set; } = new();
    }
}
