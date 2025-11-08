// ========================================
// Booksy.ServiceCatalog.Application/Queries/Reports/GetRevenueReport/GetRevenueReportQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Visitors;

namespace Booksy.ServiceCatalog.Application.Queries.Reports.GetRevenueReport
{
    public sealed class GetRevenueReportQueryHandler : IQueryHandler<GetRevenueReportQuery, RevenueReportDto>
    {
        private readonly IBookingReadRepository _bookingRepository;
        private readonly IPaymentReadRepository _paymentRepository;

        public GetRevenueReportQueryHandler(
            IBookingReadRepository bookingRepository,
            IPaymentReadRepository paymentRepository)
        {
            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        }

        public async Task<RevenueReportDto> Handle(GetRevenueReportQuery request, CancellationToken cancellationToken)
        {
            // Create the visitor with filters
            ProviderId? providerId = request.ProviderId.HasValue
                ? ProviderId.From(request.ProviderId.Value)
                : null;

            var visitor = new RevenueReportVisitor(providerId, request.StartDate, request.EndDate);

            // Get bookings using search (supports filtering)
            var (bookings, _) = await _bookingRepository.SearchBookingsAsync(
                providerId: request.ProviderId,
                customerId: null,
                serviceId: null,
                staffId: null,
                status: null,
                startDate: request.StartDate,
                endDate: request.EndDate,
                pageNumber: 1,
                pageSize: int.MaxValue, // Get all matching bookings
                cancellationToken: cancellationToken);

            // Get payments
            IReadOnlyList<Domain.Aggregates.PaymentAggregate.Payment> payments;
            if (request.ProviderId.HasValue && request.StartDate.HasValue && request.EndDate.HasValue)
            {
                payments = await _paymentRepository.GetProviderPaymentsInRangeAsync(
                    ProviderId.From(request.ProviderId.Value),
                    request.StartDate.Value,
                    request.EndDate.Value,
                    null,
                    cancellationToken);
            }
            else if (request.ProviderId.HasValue)
            {
                payments = await _paymentRepository.GetByProviderIdAsync(
                    ProviderId.From(request.ProviderId.Value),
                    cancellationToken);
            }
            else if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                payments = await _paymentRepository.GetByDateRangeAsync(
                    request.StartDate.Value,
                    request.EndDate.Value,
                    cancellationToken);
            }
            else
            {
                // Get all paid payments as a default
                payments = await _paymentRepository.GetByStatusAsync(
                    Domain.Enums.PaymentStatus.Paid,
                    cancellationToken);
            }

            // Visit all bookings and payments
            foreach (var booking in bookings)
            {
                booking.Accept(visitor);
            }

            foreach (var payment in payments)
            {
                payment.Accept(visitor);
            }

            // Get the result from the visitor
            var result = visitor.GetResult();

            // Map to DTO
            return new RevenueReportDto(
                TotalRevenue: result.TotalRevenue.Amount,
                Currency: result.TotalRevenue.Currency,
                AverageBookingValue: result.AverageBookingValue.Amount,
                TotalBookings: result.TotalBookings,
                PaidBookings: result.PaidBookings,
                RevenueByDate: result.RevenueByDate.Select(r => new DailyRevenueDto(r.Date, r.Amount, r.Currency)).ToList()
            );
        }
    }
}
