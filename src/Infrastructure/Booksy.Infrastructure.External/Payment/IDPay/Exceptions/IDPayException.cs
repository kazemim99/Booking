// ========================================
// IDPayException.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.IDPay.Exceptions
{
    /// <summary>
    /// Exception thrown when IDPay payment operations fail
    /// </summary>
    public class IDPayException : Exception
    {
        public int ErrorCode { get; }

        public IDPayException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        public IDPayException(string message, int errorCode, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
