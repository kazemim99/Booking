// ========================================
// BehpardakhtException.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.Behpardakht.Exceptions
{
    /// <summary>
    /// Base exception for Behpardakht payment gateway errors
    /// </summary>
    public class BehpardakhtException : Exception
    {
        public int ErrorCode { get; set; }

        public BehpardakhtException(string message, int errorCode = 0) : base(message)
        {
            ErrorCode = errorCode;
        }

        public BehpardakhtException(string message, Exception innerException, int errorCode = 0)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
