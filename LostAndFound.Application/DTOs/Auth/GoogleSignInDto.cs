using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Application.DTOs.Auth
{
    /// <summary>
    /// DTO for "Sign in with Google". The client obtains an ID token from Google Sign-In
    /// (e.g. via Google Sign-In SDK on mobile or gapi on web) and sends it here.
    /// </summary>
    public class GoogleSignInDto
    {
        [Required(ErrorMessage = "Google ID token is required")]
        public string IdToken { get; set; } = string.Empty;
    }
}
