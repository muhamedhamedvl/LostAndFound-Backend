using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Application.DTOs.Auth
{
    public class LogoutDto
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
