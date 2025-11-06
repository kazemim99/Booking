// ========================================
// Booksy.ServiceCatalog.Application/Queries/Payment/GetCustomerPayments/GetCustomerPaymentsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;

namespace Booksy.ServiceCatalog.Application.Queries.Payment.GetCustomerPayments
{
    public sealed class GetCustomerPaymentsQueryHandler : IQueryHandler<GetCustomerPaymentsQuery, List<PaymentSummaryDto>>
    {
        private readonly IPaymentReadRepository _paymentRepository;

        public GetCustomerPaymentsQueryHandler(IPaymentReadRepository paymentRepository)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        }

        public async Task<List<PaymentSummaryDto>> Handle(GetCustomerPaymentsQuery request, CancellationToken cancellationToken)
        {
            var customerId = UserId.From(request.CustomerId);
            var payments = await _paymentRepository.GetByCustomerIdAsync(customerId, cancellationToken);

            // Apply filters
            var filtered = payments.AsEnumerable();

            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<PaymentStatus>(request.Status, out var status))
            {
                filtered = filtered.Where(p => p.Status == status);
            }

            if (request.StartDate.HasValue)
            {
                filtered = filtered.Where(p => p.CreatedAt >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                filtered = filtered.Where(p => p.CreatedAt <= request.EndDate.Value);
            }

            return filtered
                .Select(p => new PaymentSummaryDto(
                    p.Id.Value,
                    p.BookingId?.Value,
                    p.ProviderId.Value,
                    p.Amount.Amount,
                    p.Amount.Currency,
                    p.Status.ToString(),
                    p.Method.ToString(),
                    p.Description,
                    p.CreatedAt,
                    p.CapturedAt))
                .ToList();
        }
    }
}
