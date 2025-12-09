using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Application.DTOs.Auth
{
    public class DeleteAccountDto
    {
        [Required(ErrorMessage = "Password is required to delete account")]
        public string Password { get; set; } = string.Empty;
    }
}
