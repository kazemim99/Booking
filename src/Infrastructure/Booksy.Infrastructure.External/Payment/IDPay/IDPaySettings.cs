// ========================================
// IDPaySettings.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.IDPay
{
    /// <summary>
    /// Configuration settings for IDPay payment gateway
    /// </summary>
    public class IDPaySettings
    {
        /// <summary>
        /// IDPay API Key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Whether to use sandbox mode
        /// </summary>
        public bool IsSandbox { get; set; } = true;

        /// <summary>
        /// Callback URL for payment verification
        /// </summary>
        public string CallbackUrl { get; set; } = string.Empty;

        /// <summary>
        /// IDPay API base URL
        /// </summary>
        public string ApiBaseUrl => IsSandbox
            ? "https://api.idpay.ir/v1.1"
            : "https://api.idpay.ir/v1.1";
    }
}
