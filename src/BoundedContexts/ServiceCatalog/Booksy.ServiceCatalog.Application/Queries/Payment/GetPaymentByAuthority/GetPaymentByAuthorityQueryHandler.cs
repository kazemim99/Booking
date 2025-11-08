// ========================================
// GetPaymentByAuthorityQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Queries.Payment.GetPaymentDetails;
using Booksy.ServiceCatalog.Domain.Repositories;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetPaymentByAuthority
{
    public sealed class GetPaymentByAuthorityQueryHandler : IQueryHandler<GetPaymentByAuthorityQuery, PaymentDetailsViewModel?>
    {
        private readonly IPaymentReadRepository _paymentRepository;

        public GetPaymentByAuthorityQueryHandler(IPaymentReadRepository paymentRepository)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        }

        public async Task<PaymentDetailsViewModel?> Handle(GetPaymentByAuthorityQuery request, CancellationToken cancellationToken)
        {
            var payment = await _paymentRepository.GetByAuthorityAsync(request.Authority, cancellationToken);

            if (payment == null)
                return null;

            var transactions = payment.Transactions
                .Select(t => new TransactionDto(
                    t.Id,
                    t.Type.ToString(),
                    t.Amount.Amount,
                    t.Amount.Currency,
                    t.ExternalTransactionId,
                    t.Reference,
                    t.Status,
                    t.StatusReason,
                    t.ProcessedAt,
                    t.CompletedAt))
                .ToList();

            var metadata = payment.Metadata.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty);

            return new PaymentDetailsViewModel(
                payment.Id.Value,
                payment.BookingId?.Value,
                payment.CustomerId.Value,
                payment.ProviderId.Value,
                payment.Amount.Amount,
                payment.Amount.Currency,
                payment.PaidAmount.Amount,
                payment.RefundedAmount.Amount,
                payment.Status.ToString(),
                payment.Method.ToString(),
                payment.PaymentIntentId,
                payment.PaymentMethodId,
                payment.Description,
                payment.FailureReason,
                metadata,
                transactions,
                payment.CreatedAt,
                payment.AuthorizedAt,
                payment.CapturedAt,
                payment.RefundedAt,
                payment.FailedAt,
                payment.Authority,
                payment.RefNumber,
                payment.CardPan,
                payment.Fee?.Amount,
                payment.PaymentUrl);
        }
    }
}
