// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetBookingDetails/GetBookingDetailsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetBookingDetails
{
    public sealed class GetBookingDetailsQueryHandler : IQueryHandler<GetBookingDetailsQuery, BookingDetailsViewModel?>
    {
        private readonly IBookingReadRepository _bookingRepository;
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ILogger<GetBookingDetailsQueryHandler> _logger;

        public GetBookingDetailsQueryHandler(
            IBookingReadRepository bookingRepository,
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            ILogger<GetBookingDetailsQueryHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        public async Task<BookingDetailsViewModel?> Handle(GetBookingDetailsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting booking details for {BookingId}", request.BookingId);

            var booking = await _bookingRepository.GetByIdAsync(
                BookingId.From(request.BookingId),
                cancellationToken);

            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found", request.BookingId);
                return null;
            }

            // Load provider and service for additional details
            var provider = await _providerRepository.GetByIdAsync(booking.ProviderId, cancellationToken);
            var service = await _serviceRepository.GetByIdAsync(booking.ServiceId, cancellationToken);


            return new BookingDetailsViewModel(
                BookingId: booking.Id.Value,
                CustomerId: booking.CustomerId.Value,
                ProviderId: booking.ProviderId.Value,
                ProviderName: provider?.Profile.BusinessName ?? "Unknown Provider",
                ServiceId: booking.ServiceId.Value,
                ServiceName: service?.Name ?? "Unknown Service",
                StaffId: booking.StaffId,
                StartTime: booking.TimeSlot.StartTime,
                EndTime: booking.TimeSlot.EndTime,
                DurationMinutes: booking.Duration.Value,
                Status: booking.Status.ToString(),
                TotalPrice: booking.TotalPrice.Amount,
                Currency: booking.TotalPrice.Currency,
                PaymentInfo: new PaymentInfoDto(
                    TotalAmount: booking.PaymentInfo.TotalAmount.Amount,
                    DepositAmount: booking.PaymentInfo.DepositAmount.Amount,
                    PaidAmount: booking.PaymentInfo.PaidAmount.Amount,
                    RefundedAmount: booking.PaymentInfo.RefundedAmount.Amount,
                    Status: booking.PaymentInfo.Status.ToString(),
                    IsFullyPaid: booking.PaymentInfo.IsFullyPaid(),
                    RemainingAmount: booking.PaymentInfo.GetRemainingAmount().Amount),
                CustomerNotes: booking.CustomerNotes,
                StaffNotes: booking.StaffNotes,
                CancellationReason: booking.CancellationReason,
                RequestedAt: booking.RequestedAt,
                ConfirmedAt: booking.ConfirmedAt,
                CancelledAt: booking.CancelledAt,
                CompletedAt: booking.CompletedAt,
                History: booking.History
                    .Select(h => new BookingHistoryDto(
                        h.Description,
                        h.Status.ToString(),
                        h.OccurredAt))
                    .ToList());
        }
    }
}
