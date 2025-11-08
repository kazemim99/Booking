// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/AddNotes/AddBookingNotesCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.AddNotes
{
    public sealed class AddBookingNotesCommandHandler : ICommandHandler<AddBookingNotesCommand, AddBookingNotesResult>
    {
        private readonly IBookingWriteRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AddBookingNotesCommandHandler> _logger;

        public AddBookingNotesCommandHandler(
            IBookingWriteRepository bookingRepository,
            IUnitOfWork unitOfWork,
            ILogger<AddBookingNotesCommandHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<AddBookingNotesResult> Handle(AddBookingNotesCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Adding {NoteType} notes to booking {BookingId}",
                request.IsStaffNote ? "staff" : "customer", request.BookingId);

            // Load booking
            var booking = await _bookingRepository.GetByIdAsync(
                BookingId.From(request.BookingId),
                cancellationToken);

            if (booking == null)
                throw new NotFoundException($"Booking with ID {request.BookingId} not found");

            // Add notes
            if (request.IsStaffNote)
            {
                booking.UpdateStaffNotes(request.Notes, request.AddedBy);
            }
            else
            {
                booking.UpdateCustomerNotes(request.Notes, request.AddedBy);
            }

            // Update booking
            await _bookingRepository.UpdateBookingAsync(booking, cancellationToken);

            // Commit transaction and publish events
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation("Notes added to booking {BookingId} successfully", booking.Id);

            return new AddBookingNotesResult(
                BookingId: booking.Id.Value,
                Notes: request.Notes,
                IsStaffNote: request.IsStaffNote,
                AddedAt: DateTime.UtcNow);
        }
    }
}
