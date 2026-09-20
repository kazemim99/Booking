// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/CompleteBooking/CompleteBookingCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.CompleteBooking
{
    public sealed class CompleteBookingCommandHandler : ICommandHandler<CompleteBookingCommand, CompleteBookingResult>
    {
        private readonly IBookingWriteRepository _bookingRepository;
        private readonly IBookingReminderScheduler _reminders;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<CompleteBookingCommandHandler> _logger;

        public CompleteBookingCommandHandler(
            IBookingWriteRepository bookingRepository,
            IServiceCatalogUnitOfWork unitOfWork,
            ILogger<CompleteBookingCommandHandler> logger,
            IBookingReminderScheduler reminders)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _reminders = reminders;
        }

        public async Task<CompleteBookingResult> Handle(CompleteBookingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Completing booking {BookingId}", request.BookingId);

            // Load booking
            var booking = await _bookingRepository.GetByIdAsync(
                BookingId.From(request.BookingId),
                cancellationToken);

            if (booking == null)
                throw new NotFoundException($"Booking with ID {request.BookingId} not found");

            // Complete booking
            booking.Complete(request.StaffNotes);

            // Update booking
            await _bookingRepository.UpdateBookingAsync(booking, cancellationToken);

            // The appointment is no longer going to happen, so its unsent reminders must not go out.
            await _reminders.WithdrawAsync(booking.Id.Value, cancellationToken);


            _logger.LogInformation("Booking {BookingId} completed successfully", booking.Id);
            Telemetry.BookingMetrics.BookingCompleted();

            return new CompleteBookingResult(
                BookingId: booking.Id.Value,
                Status: booking.Status.ToString(),
                CompletedAt: booking.CompletedAt!.Value);
        }
    }
}
