using LostAndFound.Application.Interfaces;
using System.Security.Cryptography;

namespace LostAndFound.Application.Services
{
    public class OtpService : IOtpService
    {
        // C4 fix: Use cryptographically secure RNG instead of System.Random
        public string GenerateOtp()
        {
            return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
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
