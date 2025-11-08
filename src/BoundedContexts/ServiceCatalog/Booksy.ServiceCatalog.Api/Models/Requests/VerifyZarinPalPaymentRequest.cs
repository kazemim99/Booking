// ========================================
// VerifyZarinPalPaymentRequest.cs
// ========================================
namespace Booksy.ServiceCatalog.API.Models.Requests
{
    /// <summary>
    /// Request model for manually verifying a ZarinPal payment
    /// </summary>
    public sealed class VerifyZarinPalPaymentRequest
    {
        /// <summary>
        /// ZarinPal authority code
        /// </summary>
        public string Authority { get; set; } = string.Empty;
    }
}
