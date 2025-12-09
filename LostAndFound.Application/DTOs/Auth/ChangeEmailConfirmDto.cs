using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Application.DTOs.Auth
{
    public class ChangeEmailConfirmDto
    {
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits")]
        public string VerificationCode { get; set; } = string.Empty;
    }
}
