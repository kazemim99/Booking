// ========================================
// Booksy.ServiceCatalog.Application/Queries/Reports/GetProviderPerformance/GetProviderPerformanceQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Visitors;

namespace Booksy.ServiceCatalog.Application.Queries.Reports.GetProviderPerformance
{
    public sealed class GetProviderPerformanceQueryHandler : IQueryHandler<GetProviderPerformanceQuery, ProviderPerformanceDto>
    {
        private readonly IBookingReadRepository _bookingRepository;
        private readonly IPaymentReadRepository _paymentRepository;

        public GetProviderPerformanceQueryHandler(
            IBookingReadRepository bookingRepository,
            IPaymentReadRepository paymentRepository)
        {
            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        }

        public async Task<ProviderPerformanceDto> Handle(GetProviderPerformanceQuery request, CancellationToken cancellationToken)
        {
            var providerId = ProviderId.From(request.ProviderId);
            var visitor = new ProviderPerformanceVisitor(providerId, request.StartDate, request.EndDate);

            // Get provider bookings
            var bookings = await _bookingRepository.GetByProviderIdAsync(providerId, cancellationToken);

            // Get provider payments
            var payments = await _paymentRepository.GetByProviderIdAsync(providerId, cancellationToken);

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
            return new ProviderPerformanceDto(
                ProviderId: result.ProviderId.Value,
                TotalBookings: result.TotalBookings,
                CompletedBookings: result.CompletedBookings,
                CancelledBookings: result.CancelledBookings,
                NoShowBookings: result.NoShowBookings,
                TotalRevenue: result.TotalRevenue.Amount,
                Currency: result.TotalRevenue.Currency,
                AverageBookingDurationMinutes: result.AverageBookingDurationMinutes,
                CompletionRate: result.CompletionRate,
                TopServices: result.TopServices
                    .Select(s => new TopServiceDto(s.ServiceId.Value, s.BookingCount))
                    .ToList()
            );
        }
    }
}
