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

        /// <summary>
        /// The gateway outcome as reported to the client on return: <c>"OK"</c> when the customer completed payment,
        /// or <c>"NOK"</c> when they cancelled/abandoned it. Defaults to <c>"OK"</c> so existing callers are
        /// unaffected. The server never trusts this as proof of payment — a value of <c>"OK"</c> still triggers a
        /// verification against ZarinPal, which is the sole authority for whether money actually moved. It exists so
        /// a cancellation can be settled promptly instead of leaving the payment Pending until reconciliation.
        /// </summary>
        public string Status { get; set; } = "OK";
    }
}
