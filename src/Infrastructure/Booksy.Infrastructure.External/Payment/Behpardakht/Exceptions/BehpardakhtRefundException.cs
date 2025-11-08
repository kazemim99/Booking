// ========================================
// BehpardakhtRefundException.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.Behpardakht.Exceptions
{
    /// <summary>
    /// Exception thrown when Behpardakht payment refund fails
    /// </summary>
    public class BehpardakhtRefundException : BehpardakhtException
    {
        public BehpardakhtRefundException(string message, int errorCode = 0)
            : base(message, errorCode)
        {
        }

        public BehpardakhtRefundException(string message, Exception innerException, int errorCode = 0)
            : base(message, innerException, errorCode)
        {
        }
    }
}
