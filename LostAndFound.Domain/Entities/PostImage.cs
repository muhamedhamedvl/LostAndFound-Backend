using System;

namespace LostAndFound.Domain.Entities
{
    public class PostImage : BaseEntity
    {
        public int PostId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        public Post? Post { get; set; }
    }
}

