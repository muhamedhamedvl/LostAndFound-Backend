using LostAndFound.Application.Interfaces;

namespace LostAndFound.Application.Services
{
    public class OtpService : IOtpService
    {
        private readonly Random _random = new();

        public string GenerateOtp()
        {
            return _random.Next(100000, 999999).ToString();
        }

        public bool ValidateOtp(string otp, string storedOtp, DateTime? expiry)
        {
            if (string.IsNullOrEmpty(otp) || string.IsNullOrEmpty(storedOtp))
                return false;

            if (expiry == null || DateTime.UtcNow > expiry.Value)
                return false;

            return otp.Equals(storedOtp, StringComparison.OrdinalIgnoreCase);
        }

        public DateTime GetOtpExpiry()
        {
            return DateTime.UtcNow.AddMinutes(10); 
        }
    }
}
