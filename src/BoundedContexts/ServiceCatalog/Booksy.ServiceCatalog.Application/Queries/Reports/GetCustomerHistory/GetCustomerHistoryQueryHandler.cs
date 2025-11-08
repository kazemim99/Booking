// ========================================
// Booksy.ServiceCatalog.Application/Queries/Reports/GetCustomerHistory/GetCustomerHistoryQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.Visitors;

namespace Booksy.ServiceCatalog.Application.Queries.Reports.GetCustomerHistory
{
    public sealed class GetCustomerHistoryQueryHandler : IQueryHandler<GetCustomerHistoryQuery, CustomerHistoryDto>
    {
        private readonly IBookingReadRepository _bookingRepository;
        private readonly IPaymentReadRepository _paymentRepository;

        public GetCustomerHistoryQueryHandler(
            IBookingReadRepository bookingRepository,
            IPaymentReadRepository paymentRepository)
        {
            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        }

        public async Task<CustomerHistoryDto> Handle(GetCustomerHistoryQuery request, CancellationToken cancellationToken)
        {
            var customerId = UserId.From(request.CustomerId);
            var visitor = new CustomerBookingHistoryVisitor(customerId);

            // Get customer bookings
            var bookings = await _bookingRepository.GetByCustomerIdAsync(customerId, cancellationToken);

            // Get customer payments
            var payments = await _paymentRepository.GetByCustomerIdAsync(customerId, cancellationToken);

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
            return new CustomerHistoryDto(
                CustomerId: result.CustomerId.Value,
                TotalBookings: result.TotalBookings,
                CompletedBookings: result.CompletedBookings,
                CancelledBookings: result.CancelledBookings,
                TotalSpent: result.TotalSpent.Amount,
                Currency: result.TotalSpent.Currency,
                FavoriteProviders: result.FavoriteProviders
                    .Select(p => new FavoriteProviderDto(p.ProviderId.Value, p.BookingCount))
                    .ToList()
            );
        }
    }
}
