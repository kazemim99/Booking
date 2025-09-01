using Booksy.Infrastructure.External.Payment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

public class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentGateway> _logger;

    public StripePaymentGateway(
        IOptions<StripeSettings> settings,
        ILogger<StripePaymentGateway> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(
        PaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100), // Convert to cents
                Currency = request.Currency.ToLower(),
                PaymentMethod = request.PaymentMethodId,
                Customer = request.CustomerId,
                Description = request.Description,
                Metadata = request.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()),
                Confirm = true
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options, cancellationToken: cancellationToken);

            return new PaymentResult
            {
                IsSuccessful = paymentIntent.Status == "succeeded",
                PaymentId = paymentIntent.Id,
                Status = paymentIntent.Status
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe payment failed");

            return new PaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                ErrorCode = ex.StripeError?.Code
            };
        }
    }

    public async Task<RefundResult> RefundPaymentAsync(
        string paymentId,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentId,
                Amount = (long)(amount * 100),
                Reason = reason
            };

            var service = new RefundService();
            var refund = await service.CreateAsync(options, cancellationToken: cancellationToken);

            return new RefundResult
            {
                IsSuccessful = refund.Status == "succeeded",
                RefundId = refund.Id,
                Amount = refund.Amount / 100m
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe refund failed");

            return new RefundResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PaymentDetails> GetPaymentDetailsAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentId, cancellationToken: cancellationToken);

            return new PaymentDetails
            {
                PaymentId = paymentIntent.Id,
                Amount = paymentIntent.Amount / 100m,
                Currency = paymentIntent.Currency.ToUpper(),
                Status = paymentIntent.Status,
                CreatedAt = paymentIntent.Created,
                Metadata = paymentIntent.Metadata?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get payment details");
            throw;
        }
    }

    public async Task<PaymentIntent> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = currency.ToLower(),
                Metadata = metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
            };

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options, cancellationToken: cancellationToken);

            return new PaymentIntent
            {
                Id = intent.Id,
                ClientSecret = intent.ClientSecret,
                Amount = intent.Amount / 100m,
                Currency = intent.Currency.ToUpper(),
                Status = intent.Status
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create payment intent");
            throw;
        }
    }

    public async Task<PaymentResult> ConfirmPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.ConfirmAsync(paymentIntentId, cancellationToken: cancellationToken);

            return new PaymentResult
            {
                IsSuccessful = paymentIntent.Status == "succeeded",
                PaymentId = paymentIntent.Id,
                Status = paymentIntent.Status
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to confirm payment intent");

            return new PaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                ErrorCode = ex.StripeError?.Code
            };
        }
    }
}
