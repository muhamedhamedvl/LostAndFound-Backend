using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostAndFound.Domain.Entities
{
    public class Photo : BaseEntity
    {
        public string Url { get; set; } = string.Empty; 
        public string? PublicId { get; set; }           

        public int PostId { get; set; }                
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public Post? Post { get; set; }
    }
}
