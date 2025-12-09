using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Application.DTOs.Auth
{
    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
