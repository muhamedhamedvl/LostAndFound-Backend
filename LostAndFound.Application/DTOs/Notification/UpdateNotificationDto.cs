using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostAndFound.Application.DTOs.Notification
{
    public class UpdateNotificationDto
    {
        public int Id { get; set; }
        public bool IsRead { get; set; }
    }
}
