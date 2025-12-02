namespace LostAndFound.Application.Interfaces
{
    public interface IOtpService
    {
        string GenerateOtp();
        bool ValidateOtp(string otp, string storedOtp, DateTime? expiry);
        DateTime GetOtpExpiry();
    }
}
