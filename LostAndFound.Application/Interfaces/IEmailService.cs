namespace LostAndFound.Application.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task<bool> SendVerificationCodeEmailAsync(string to, string name, string verificationCode);
        Task<bool> SendPasswordResetEmailAsync(string to, string name, string resetToken);
        Task<bool> SendEmailChangeVerificationAsync(string to, string name, string verificationCode, string newEmail);
    }
}
