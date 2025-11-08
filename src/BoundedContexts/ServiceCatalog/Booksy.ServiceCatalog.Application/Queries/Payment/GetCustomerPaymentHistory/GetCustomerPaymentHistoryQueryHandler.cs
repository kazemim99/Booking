// ========================================
// GetCustomerPaymentHistoryQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Repositories;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetCustomerPaymentHistory
{
    public sealed class GetCustomerPaymentHistoryQueryHandler
        : IQueryHandler<GetCustomerPaymentHistoryQuery, PaymentHistoryViewModel>
    {
        private readonly IPaymentReadRepository _paymentRepository;

        public GetCustomerPaymentHistoryQueryHandler(IPaymentReadRepository paymentRepository)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        }

        public async Task<PaymentHistoryViewModel> Handle(
            GetCustomerPaymentHistoryQuery request,
            CancellationToken cancellationToken)
        {
            var customerId = UserId.From(request.CustomerId);
            var skip = (request.Page - 1) * request.PageSize;

            var (payments, totalCount) = await _paymentRepository.GetCustomerPaymentHistoryAsync(
                customerId,
                request.StartDate,
                request.EndDate,
                skip,
                request.PageSize,
                cancellationToken);

            var paymentItems = payments.Select(p => new PaymentHistoryItemDto(
                p.Id.Value,
                p.BookingId?.Value,
                p.Amount.Amount,
                p.Amount.Currency,
                p.Status.ToString(),
                p.Method.ToString(),
                p.Description,
                p.RefNumber,
                p.CardPan,
                p.CreatedAt,
                p.CapturedAt)).ToList();

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            return new PaymentHistoryViewModel(
                paymentItems,
                totalCount,
                request.Page,
                request.PageSize,
                totalPages);
        }
    }
}
