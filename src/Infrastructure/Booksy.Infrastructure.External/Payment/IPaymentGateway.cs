using Booksy.Infrastructure.External.Payment;

public interface IPaymentGateway
{
    /// <summary>
    /// Processes a payment
    /// </summary>
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refunds a payment
    /// </summary>
    Task<RefundResult> RefundPaymentAsync(string paymentId, decimal amount, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payment details
    /// </summary>
    Task<PaymentDetails> GetPaymentDetailsAsync(string paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a payment intent
    /// </summary>
    Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string currency, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a payment intent
    /// </summary>
    Task<PaymentResult> ConfirmPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default);
}
