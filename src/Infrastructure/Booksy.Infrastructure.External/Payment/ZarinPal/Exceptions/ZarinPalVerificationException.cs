// ========================================
// ZarinPalVerificationException.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.ZarinPal.Exceptions
{
    /// <summary>
    /// Exception for ZarinPal payment verification errors
    /// </summary>
    public class ZarinPalVerificationException : ZarinPalException
    {
        public string Authority { get; }

        public ZarinPalVerificationException(string authority, int errorCode, string message)
            : base(errorCode, message)
        {
            Authority = authority;
        }

        public ZarinPalVerificationException(string authority, int errorCode, string message, Exception innerException)
            : base(errorCode, message, innerException)
        {
            Authority = authority;
        }
    }
}
