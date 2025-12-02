using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostAndFound.Domain.Entities
{
    public class Reward : BaseEntity
    {
        public int PostId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public bool IsClaimed { get; set; } = false;
        public Post? Post { get; set; }
    }
}
