using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostAndFound.Application.DTOs.Item
{
    public class PhotoDto
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? PublicId { get; set; }
        public int PostId { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
