using System;
using System.Collections.Generic;

namespace LostAndFound.Domain.Entities
{
    public class SubCategory : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
