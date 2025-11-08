// ========================================
// ZarinPalSettings.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.ZarinPal
{
    /// <summary>
    /// Configuration settings for ZarinPal payment gateway
    /// </summary>
    public class ZarinPalSettings
    {
        /// <summary>
        /// ZarinPal merchant ID
        /// </summary>
        public string MerchantId { get; set; } = string.Empty;

        /// <summary>
        /// Whether to use sandbox mode (for testing)
        /// </summary>
        public bool IsSandbox { get; set; } = true;

        /// <summary>
        /// Callback URL for payment verification
        /// </summary>
        public string CallbackUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets the base URL based on environment (sandbox or production)
        /// </summary>
        public string BaseUrl => IsSandbox
            ? "https://sandbox.zarinpal.com"
            : "https://api.zarinpal.com";

        /// <summary>
        /// Gets the payment gateway URL for user redirection
        /// </summary>
        public string StartPayUrl => IsSandbox
            ? "https://sandbox.zarinpal.com/pg/StartPay/"
            : "https://www.zarinpal.com/pg/StartPay/";
    }
}
