// ========================================
// ZarinPalException.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.ZarinPal.Exceptions
{
    /// <summary>
    /// Base exception for ZarinPal payment gateway errors
    /// </summary>
    public class ZarinPalException : Exception
    {
        public int ErrorCode { get; }

        public ZarinPalException(int errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public ZarinPalException(int errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Factory method to create exception from error code
        /// </summary>
        public static ZarinPalException FromErrorCode(int errorCode)
        {
            var message = errorCode switch
            {
                -9 => "Validation error in submitted data",
                -10 => "Invalid merchant ID or IP/domain not registered",
                -11 => "Invalid merchant credentials",
                -15 => "Payment gateway access suspended",
                -16 => "Invalid credibility level",
                -30 => "Invalid merchant terminal code",
                -31 => "Terminal inactive",
                -50 => "Amount must be above 1000 Rials",
                -51 => "Transaction already verified",
                -52 => "Transaction not found for verification",
                -53 => "Transaction verification unsuccessful",
                -54 => "Transaction verification time expired",
                _ => $"ZarinPal error (code: {errorCode})"
            };

            return new ZarinPalException(errorCode, message);
        }
    }
}
