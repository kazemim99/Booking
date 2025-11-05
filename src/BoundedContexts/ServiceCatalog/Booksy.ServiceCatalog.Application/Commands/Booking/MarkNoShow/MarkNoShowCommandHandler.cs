// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/MarkNoShow/MarkNoShowCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.MarkNoShow
{
    /// <summary>
    /// Handler for marking a booking as no-show
    /// </summary>
    public sealed class MarkNoShowCommandHandler : ICommandHandler<MarkNoShowCommand, MarkNoShowResult>
    {
        private readonly IBookingWriteRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MarkNoShowCommandHandler> _logger;

        public MarkNoShowCommandHandler(
            IBookingWriteRepository bookingRepository,
            IUnitOfWork unitOfWork,
            ILogger<MarkNoShowCommandHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<MarkNoShowResult> Handle(MarkNoShowCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Marking booking {BookingId} as no-show", request.BookingId);

            // Load booking
            var booking = await _bookingRepository.GetByIdAsync(
                BookingId.From(request.BookingId),
                cancellationToken);

            if (booking == null)
                throw new NotFoundException($"Booking with ID {request.BookingId} not found");

            // Mark as no-show
            booking.MarkAsNoShow(request.Notes);

            // Update booking
            await _bookingRepository.UpdateBookingAsync(booking, cancellationToken);

            // Commit transaction and publish events
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation("Booking {BookingId} marked as no-show successfully", booking.Id);

            return new MarkNoShowResult(
                BookingId: booking.Id.Value,
                Status: booking.Status.ToString(),
                MarkedAt: DateTime.UtcNow);
        }
    }
}
