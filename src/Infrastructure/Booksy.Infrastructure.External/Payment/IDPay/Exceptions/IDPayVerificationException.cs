// ========================================
// IDPayVerificationException.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.IDPay.Exceptions
{
    /// <summary>
    /// Exception thrown when IDPay payment verification fails
    /// </summary>
    public class IDPayVerificationException : IDPayException
    {
        public IDPayVerificationException(string message, int errorCode)
            : base(message, errorCode)
        {
        }

        public IDPayVerificationException(string message, int errorCode, Exception innerException)
            : base(message, errorCode, innerException)
        {
        }
    }
}
