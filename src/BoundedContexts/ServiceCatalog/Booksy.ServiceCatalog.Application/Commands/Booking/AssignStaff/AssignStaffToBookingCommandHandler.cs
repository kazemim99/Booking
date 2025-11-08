// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/AssignStaff/AssignStaffToBookingCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.AssignStaff
{
    public sealed class AssignStaffToBookingCommandHandler : ICommandHandler<AssignStaffToBookingCommand, AssignStaffToBookingResult>
    {
        private readonly IBookingWriteRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignStaffToBookingCommandHandler> _logger;

        public AssignStaffToBookingCommandHandler(
            IBookingWriteRepository bookingRepository,
            IUnitOfWork unitOfWork,
            ILogger<AssignStaffToBookingCommandHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<AssignStaffToBookingResult> Handle(AssignStaffToBookingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Assigning staff {StaffId} to booking {BookingId}",
                request.StaffId, request.BookingId);

            // Load booking
            var booking = await _bookingRepository.GetByIdAsync(
                BookingId.From(request.BookingId),
                cancellationToken);

            if (booking == null)
                throw new NotFoundException($"Booking with ID {request.BookingId} not found");

            var previousStaffId = booking.StaffId;

            // Assign staff
            booking.AssignStaff(request.StaffId);

            // Update booking
            await _bookingRepository.UpdateBookingAsync(booking, cancellationToken);

            // Commit transaction and publish events
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation("Staff {StaffId} assigned to booking {BookingId} successfully",
                request.StaffId, booking.Id);

            return new AssignStaffToBookingResult(
                BookingId: booking.Id.Value,
                PreviousStaffId: previousStaffId,
                NewStaffId: booking.StaffId,
                AssignedAt: DateTime.UtcNow);
        }
    }
}
