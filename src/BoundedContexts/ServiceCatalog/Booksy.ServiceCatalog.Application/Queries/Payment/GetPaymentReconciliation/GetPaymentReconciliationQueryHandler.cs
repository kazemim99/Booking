// ========================================
// GetPaymentReconciliationQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetPaymentReconciliation
{
    public sealed class GetPaymentReconciliationQueryHandler
        : IQueryHandler<GetPaymentReconciliationQuery, ReconciliationReportViewModel>
    {
        private readonly IPaymentReadRepository _paymentRepository;

        public GetPaymentReconciliationQueryHandler(IPaymentReadRepository paymentRepository)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        }

        public async Task<ReconciliationReportViewModel> Handle(
            GetPaymentReconciliationQuery request,
            CancellationToken cancellationToken)
        {
            var payments = await _paymentRepository.GetPaymentsForReconciliationAsync(
                request.StartDate,
                request.EndDate,
                cancellationToken);

            var paymentItems = payments.Select(p => new ReconciliationItemDto(
                p.Id.Value,
                p.BookingId?.Value,
                p.CustomerId.Value,
                p.ProviderId.Value,
                p.Amount.Amount,
                p.RefundedAmount.Amount,
                p.GetNetAmount().Amount,
                p.Status.ToString(),
                p.Method.ToString(),
                p.RefNumber,
                p.Authority,
                p.CapturedAt ?? DateTime.UtcNow)).ToList();

            var totalAmount = payments.Sum(p => p.PaidAmount.Amount);
            var totalRefunded = payments.Sum(p => p.RefundedAmount.Amount);
            var netAmount = totalAmount - totalRefunded;
            var paidCount = payments.Count(p => p.Status == PaymentStatus.Paid);
            var refundedCount = payments.Count(p => p.Status == PaymentStatus.Refunded);
            var partiallyRefundedCount = payments.Count(p => p.Status == PaymentStatus.PartiallyRefunded);

            var summary = new ReconciliationSummaryDto(
                payments.Count,
                totalAmount,
                totalRefunded,
                netAmount,
                paidCount,
                refundedCount,
                partiallyRefundedCount,
                "IRR");

            return new ReconciliationReportViewModel(
                request.StartDate,
                request.EndDate,
                paymentItems,
                summary);
        }
    }
}
