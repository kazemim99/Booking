using Booksy.Infrastructure.External.Payment;

namespace Booksy.Host.IntegrationTests.Infrastructure.Fakes;

/// <summary>
/// Deterministic, no-network <see cref="IPaymentGateway"/> for the integration tests. Every
/// operation succeeds and returns ids the handlers can persist, so a test exercises the
/// application's own behaviour (payment lifecycle, ledger, payouts, refunds) instead of the
/// real ZarinPal gateway's reaction to a placeholder merchant id — which is what every
/// payment test hit before this existed (FOLLOW-UPS #31: "The merchant id must be at least
/// 36 characters"). Gateway-specific behaviour (ZarinPal/Behpardakht callbacks, verification)
/// remains covered by those gateways' own tests and is not faked here.
/// </summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    private static string NewId(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentResult
        {
            IsSuccessful = true,
            PaymentId = NewId("fake_pay"),
            Status = "succeeded",
        });

    public Task<RefundResult> RefundPaymentAsync(string paymentId, decimal amount, string reason, CancellationToken cancellationToken = default) =>
        Task.FromResult(new RefundResult
        {
            IsSuccessful = true,
            RefundId = NewId("fake_ref"),
            Amount = amount,
        });

    public Task<PaymentDetails> GetPaymentDetailsAsync(string paymentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentDetails
        {
            PaymentId = paymentId,
            Status = "succeeded",
            CreatedAt = DateTime.UtcNow,
        });

    public Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string currency, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentIntent
        {
            Id = NewId("fake_pi"),
            ClientSecret = NewId("fake_secret"),
            Amount = amount,
            Currency = currency,
            Status = "requires_confirmation",
        });

    public Task<PaymentResult> ConfirmPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentResult
        {
            IsSuccessful = true,
            PaymentId = paymentIntentId,
            Status = "succeeded",
        });

    public Task<PayoutResult> CreatePayoutAsync(PayoutRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PayoutResult
        {
            IsSuccessful = true,
            PayoutId = NewId("fake_po"),
            Status = "paid",
            Amount = request.Amount,
            Currency = request.Currency,
            ArrivalDate = DateTime.UtcNow,
        });

    public Task<PayoutDetails> GetPayoutDetailsAsync(string payoutId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PayoutDetails
        {
            PayoutId = payoutId,
            Status = "paid",
            Currency = "IRR",
            CreatedAt = DateTime.UtcNow,
            ArrivalDate = DateTime.UtcNow,
        });
}
