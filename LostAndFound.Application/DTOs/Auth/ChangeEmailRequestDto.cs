using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Application.DTOs.Auth
{
    public class ChangeEmailRequestDto
    {
        [Required(ErrorMessage = "New email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string NewEmail { get; set; } = string.Empty;
    }
}
