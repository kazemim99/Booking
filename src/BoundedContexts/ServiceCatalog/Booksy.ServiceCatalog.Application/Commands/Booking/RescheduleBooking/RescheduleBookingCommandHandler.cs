// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/RescheduleBooking/RescheduleBookingCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.DomainServices;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.RescheduleBooking
{
    /// <summary>
    /// Handler for rescheduling an existing booking
    /// </summary>
    public sealed class RescheduleBookingCommandHandler : ICommandHandler<RescheduleBookingCommand, RescheduleBookingResult>
    {
        private readonly IBookingWriteRepository _bookingWriteRepository;
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly IAvailabilityService _availabilityService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RescheduleBookingCommandHandler> _logger;

        public RescheduleBookingCommandHandler(
            IBookingWriteRepository bookingWriteRepository,
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            IAvailabilityService availabilityService,
            IUnitOfWork unitOfWork,
            ILogger<RescheduleBookingCommandHandler> logger)
        {
            _bookingWriteRepository = bookingWriteRepository;
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _availabilityService = availabilityService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<RescheduleBookingResult> Handle(RescheduleBookingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Rescheduling booking {BookingId} to {NewStartTime}",
                request.BookingId, request.NewStartTime);

            // Load existing booking
            var existingBooking = await _bookingWriteRepository.GetByIdAsync(
                BookingId.From(request.BookingId),
                cancellationToken);

            if (existingBooking == null)
                throw new NotFoundException($"Booking with ID {request.BookingId} not found");

            // Load provider and service for validation
            var provider = await _providerRepository.GetByIdAsync(
                existingBooking.ProviderId,
                cancellationToken);

            if (provider == null)
                throw new NotFoundException($"Provider with ID {existingBooking.ProviderId} not found");

            var service = await _serviceRepository.GetByIdAsync(
                existingBooking.ServiceId,
                cancellationToken);

            if (service == null)
                throw new NotFoundException($"Service with ID {existingBooking.ServiceId} not found");

            // Determine staff ID (use new staff if provided, otherwise keep current)
            var newStaffId = request.NewStaffId ?? existingBooking.StaffId;

            // Get staff member
            var staff = provider.Staff.FirstOrDefault(s => s.Id == newStaffId);
            if (staff == null)
                throw new NotFoundException($"Staff member with ID {newStaffId} not found");

            // Validate availability for new time slot
            var isAvailable = await _availabilityService.IsTimeSlotAvailableAsync(
                provider,
                service,
                staff,
                request.NewStartTime,
                service.Duration,
                cancellationToken);

            if (!isAvailable)
                throw new ConflictException("The requested time slot is not available");

            // Validate booking constraints for new time
            var validationResult = await _availabilityService.ValidateBookingConstraintsAsync(
                provider,
                service,
                request.NewStartTime,
                cancellationToken);

            if (!validationResult.IsValid)
                throw new ConflictException($"Booking validation failed: {string.Join(", ", validationResult.Errors)}");

            // Reschedule the booking (returns new booking)
            var newBooking = existingBooking.Reschedule(
                request.NewStartTime,
                newStaffId,
                request.Reason);

            // Update existing booking (marked as rescheduled)
            await _bookingWriteRepository.UpdateBookingAsync(existingBooking, cancellationToken);

            // Save new booking
            await _bookingWriteRepository.SaveBookingAsync(newBooking, cancellationToken);

            // Commit transaction and publish events
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation(
                "Booking {OldBookingId} rescheduled successfully. New booking: {NewBookingId}",
                existingBooking.Id, newBooking.Id);

            return new RescheduleBookingResult(
                OldBookingId: existingBooking.Id.Value,
                NewBookingId: newBooking.Id.Value,
                NewStartTime: newBooking.TimeSlot.StartTime,
                NewEndTime: newBooking.TimeSlot.EndTime,
                Status: newBooking.Status.ToString());
        }
    }
}
