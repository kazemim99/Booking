// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/ConfirmBooking/ConfirmBookingCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.ConfirmBooking
{
    public sealed class ConfirmBookingCommandHandler : ICommandHandler<ConfirmBookingCommand, ConfirmBookingResult>
    {
        private readonly IBookingWriteRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ConfirmBookingCommandHandler> _logger;

        public ConfirmBookingCommandHandler(
            IBookingWriteRepository bookingRepository,
            IUnitOfWork unitOfWork,
            ILogger<ConfirmBookingCommandHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ConfirmBookingResult> Handle(ConfirmBookingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Confirming booking {BookingId}", request.BookingId);

            // Load booking
            var booking = await _bookingRepository.GetByIdAsync(
                BookingId.From(request.BookingId),
                cancellationToken);

            if (booking == null)
                throw new NotFoundException($"Booking with ID {request.BookingId} not found");

            // Process payment if payment intent provided
            if (!string.IsNullOrEmpty(request.PaymentIntentId))
            {
                booking.ProcessDepositPayment(request.PaymentIntentId);
            }

            // Confirm booking
            booking.Confirm();

            // Update booking
            await _bookingRepository.UpdateBookingAsync(booking, cancellationToken);

            // Commit transaction and publish events
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation("Booking {BookingId} confirmed successfully", booking.Id);

            return new ConfirmBookingResult(
                BookingId: booking.Id.Value,
                Status: booking.Status.ToString(),
                ConfirmedAt: booking.ConfirmedAt!.Value);
        }
    }
}
