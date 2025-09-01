// ========================================
// Security Services
// ========================================

// Booksy.UserManagement.Infrastructure/Services/Security/PasswordHasher.cs
using OtpNet;
using System.Security.Cryptography;

namespace Booksy.UserManagement.Infrastructure.Services.Security
{
    public class TwoFactorAuthenticationService
    {
        private const string Issuer = "Booksy";

        public string GenerateSecretKey()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(key);
        }

        public string GenerateQrCodeUri(string email, string secretKey)
        {
            return $"otpauth://totp/{Issuer}:{email}?secret={secretKey}&issuer={Issuer}";
        }

        public bool ValidateCode(string secretKey, string code)
        {
            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(code))
                return false;

            try
            {
                var key = Base32Encoding.ToBytes(secretKey);
                var totp = new Totp(key);

                // Check current code and allow for time drift (30 seconds before/after)
                return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
            }
            catch
            {
                return false;
            }
        }

        public string GenerateBackupCodes(int count = 10)
        {
            var codes = new List<string>();

            for (int i = 0; i < count; i++)
            {
                var bytes = new byte[4];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(bytes);
                var code = BitConverter.ToUInt32(bytes, 0).ToString("D8");
                codes.Add($"{code[..4]}-{code[4..]}");
            }

            return string.Join(Environment.NewLine, codes);
        }
    }
}

