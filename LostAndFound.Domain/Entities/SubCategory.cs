using System;
using System.Collections.Generic;

namespace LostAndFound.Domain.Entities
{
    public class SubCategory : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CategoryId { get; set; } // Foreign key to Category (Main Category)
        public Category Category { get; set; } = null!;
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}

