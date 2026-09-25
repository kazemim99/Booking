// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Booking/CompleteBooking/CompleteBookingCommandHandler.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Booking.CompleteBooking
{
    public sealed class CompleteBookingCommandHandler : ICommandHandler<CompleteBookingCommand, CompleteBookingResult>
    {
        private readonly IBookingWriteRepository _bookingRepository;
        private readonly BookingCompletionFollowUp _followUp;
        private readonly ILogger<CompleteBookingCommandHandler> _logger;

        public CompleteBookingCommandHandler(
            IBookingWriteRepository bookingRepository,
            BookingCompletionFollowUp followUp,
            ILogger<CompleteBookingCommandHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _followUp = followUp;
            _logger = logger;
        }

        public async Task<CompleteBookingResult> Handle(CompleteBookingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Completing booking {BookingId}", request.BookingId);

            // Load booking
            var booking = await _bookingRepository.GetByIdAsync(
                BookingId.From(request.BookingId),
                cancellationToken);

            if (booking == null)
                throw new NotFoundException("این نوبت پیدا نشد.");

            // Complete booking
            booking.Complete(request.StaffNotes);

            // Update booking
            await _bookingRepository.UpdateBookingAsync(booking, cancellationToken);

            // Reminders withdrawn, thank-you, review request and its follow-up — the same as when a booking
            // completes by itself (AutoCompleteBookingCommand).
            await _followUp.RaiseAsync(booking, DateTime.UtcNow, cancellationToken);

            _logger.LogInformation("Booking {BookingId} completed successfully", booking.Id);
            Telemetry.BookingMetrics.BookingCompleted();

            return new CompleteBookingResult(
                BookingId: booking.Id.Value,
                Status: booking.Status.ToString(),
                CompletedAt: booking.CompletedAt!.Value);
        }
    }
}
