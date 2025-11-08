// ========================================
// BehpardakhtVerificationException.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.Behpardakht.Exceptions
{
    /// <summary>
    /// Exception thrown when Behpardakht payment verification fails
    /// </summary>
    public class BehpardakhtVerificationException : BehpardakhtException
    {
        public BehpardakhtVerificationException(string message, int errorCode = 0)
            : base(message, errorCode)
        {
        }

        public BehpardakhtVerificationException(string message, Exception innerException, int errorCode = 0)
            : base(message, innerException, errorCode)
        {
        }
    }
}
