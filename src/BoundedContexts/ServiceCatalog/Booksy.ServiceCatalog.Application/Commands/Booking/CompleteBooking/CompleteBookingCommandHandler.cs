// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/CompleteBooking/CompleteBookingCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
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
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<CompleteBookingCommandHandler> _logger;

        public CompleteBookingCommandHandler(
            IBookingWriteRepository bookingRepository,
            IServiceCatalogUnitOfWork unitOfWork,
            ILogger<CompleteBookingCommandHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
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
                throw new NotFoundException($"Booking with ID {request.BookingId} not found");

            // Complete booking
            booking.Complete(request.StaffNotes);

            // Update booking
            await _bookingRepository.UpdateBookingAsync(booking, cancellationToken);


            _logger.LogInformation("Booking {BookingId} completed successfully", booking.Id);
            Telemetry.BookingMetrics.BookingCompleted();

            return new CompleteBookingResult(
                BookingId: booking.Id.Value,
                Status: booking.Status.ToString(),
                CompletedAt: booking.CompletedAt!.Value);
        }
    }
}
