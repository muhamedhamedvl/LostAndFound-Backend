using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Application.DTOs.Notification
{
    public class RegisterDeviceTokenDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(android|ios|web)$", ErrorMessage = "Platform must be 'android', 'ios', or 'web'")]
        public string Platform { get; set; } = string.Empty;
    }
}
