// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payment/GetPaymentDetails/GetPaymentDetailsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetPaymentDetails
{
    public sealed class GetPaymentDetailsQueryHandler : IQueryHandler<GetPaymentDetailsQuery, PaymentDetailsViewModel?>
    {
        private readonly IPaymentReadRepository _paymentRepository;

        public GetPaymentDetailsQueryHandler(IPaymentReadRepository paymentRepository)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        }

        public async Task<PaymentDetailsViewModel?> Handle(GetPaymentDetailsQuery request, CancellationToken cancellationToken)
        {
            var paymentId = PaymentId.From(request.PaymentId);
            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);

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
                payment.CreatedAt,
                payment.AuthorizedAt,
                payment.CapturedAt,
                payment.RefundedAt,
                payment.FailedAt,
                transactions,
                payment.Metadata);
        }
    }
}
