using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Application.Commands.Booking.CompleteBooking;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Booking.AutoCompleteBooking
{
    /// <summary>
    /// One confirmed booking completes by itself, 12 hours after its end, because the salon marked it neither done nor
    /// no-show (openspec/changes/_inline/reviews-and-reschedule-round2 D3). Sent by the auto-completion job, one per
    /// booking so each gets its own transaction. <paramref name="UtcNow"/> is the job's clock.
    /// </summary>
    /// <returns>True when this command completed it; false when it was no longer due (another host, or the salon, got
    /// there first) — never an error, so a pass racing a salon's own «انجام شد» or «نیامد» is harmless.</returns>
    public sealed record AutoCompleteBookingCommand(Guid BookingId, DateTime UtcNow) : ICommand<bool>
    {
        public Guid? IdempotencyKey => null;
    }

    public sealed class AutoCompleteBookingCommandHandler : ICommandHandler<AutoCompleteBookingCommand, bool>
    {
        private readonly IBookingWriteRepository _bookings;
        private readonly BookingCompletionFollowUp _followUp;
        private readonly ILogger<AutoCompleteBookingCommandHandler> _logger;

        public AutoCompleteBookingCommandHandler(
            IBookingWriteRepository bookings,
            BookingCompletionFollowUp followUp,
            ILogger<AutoCompleteBookingCommandHandler> logger)
        {
            _bookings = bookings;
            _followUp = followUp;
            _logger = logger;
        }

        public async Task<bool> Handle(AutoCompleteBookingCommand request, CancellationToken cancellationToken)
        {
            var booking = await _bookings.GetByIdAsync(BookingId.From(request.BookingId), cancellationToken);
            if (booking is null
                || booking.Status != BookingStatus.Confirmed
                || !BookingAutoCompletion.IsDue(booking.TimeSlot.EndTime, request.UtcNow))
                return false;

            booking.CompleteAutomatically(request.UtcNow);
            await _bookings.UpdateBookingAsync(booking, cancellationToken);

            // Everything a salon's own «انجام شد» sets off — the review request above all.
            await _followUp.RaiseAsync(booking, request.UtcNow, cancellationToken);

            _logger.LogInformation(
                "Booking {BookingId} completed automatically {Hours} h after its end",
                booking.Id, BookingAutoCompletion.After.TotalHours);
            Telemetry.BookingMetrics.BookingCompleted();
            return true;
        }
    }
}
