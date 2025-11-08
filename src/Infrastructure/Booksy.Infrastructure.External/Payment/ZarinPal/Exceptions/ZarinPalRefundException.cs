// ========================================
// ZarinPalRefundException.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.ZarinPal.Exceptions
{
    /// <summary>
    /// Exception for ZarinPal refund errors
    /// </summary>
    public class ZarinPalRefundException : ZarinPalException
    {
        public string Authority { get; }
        public decimal Amount { get; }

        public ZarinPalRefundException(string authority, decimal amount, int errorCode, string message)
            : base(errorCode, message)
        {
            Authority = authority;
            Amount = amount;
        }

        public ZarinPalRefundException(string authority, decimal amount, int errorCode, string message, Exception innerException)
            : base(errorCode, message, innerException)
        {
            Authority = authority;
            Amount = amount;
        }
    }
}
