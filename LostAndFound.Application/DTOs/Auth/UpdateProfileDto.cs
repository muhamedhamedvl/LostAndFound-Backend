using Microsoft.AspNetCore.Http;

namespace LostAndFound.Application.DTOs.Auth
{
    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; } 
        public IFormFile? ProfilePicture { get; set; }
    }
}

