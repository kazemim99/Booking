// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/RescheduleBooking/RescheduleBookingCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Application.Services;
using Booksy.ServiceCatalog.Domain.DomainServices;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using ProviderAggregate = Booksy.ServiceCatalog.Domain.Aggregates.Provider;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.RescheduleBooking
{
    /// <summary>
    /// Handler for rescheduling an existing booking
    /// Atomically releases old availability slot and books new slot
    /// </summary>
    public sealed class RescheduleBookingCommandHandler : ICommandHandler<RescheduleBookingCommand, RescheduleBookingResult>
    {
        private readonly IBookingWriteRepository _bookingWriteRepository;
        private readonly IBookingReadRepository _bookingReadRepository;
        private readonly IProviderReadRepository _providerRepository;
        private readonly IBookableResourceResolver _resourceResolver;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly IProviderAvailabilityWriteRepository _availabilityWriteRepository;
        private readonly IAvailabilityService _availabilityService;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<RescheduleBookingCommandHandler> _logger;

        /// <summary>Gap kept after an appointment, matching booking creation.</summary>
        private const int BufferMinutes = 15;

        public RescheduleBookingCommandHandler(
            IBookingWriteRepository bookingWriteRepository,
            IBookingReadRepository bookingReadRepository,
            IProviderReadRepository providerRepository,
            IBookableResourceResolver resourceResolver,
            IServiceReadRepository serviceRepository,
            IProviderAvailabilityWriteRepository availabilityWriteRepository,
            IAvailabilityService availabilityService,
            IServiceCatalogUnitOfWork unitOfWork,
            ILogger<RescheduleBookingCommandHandler> logger)
        {
            _bookingWriteRepository = bookingWriteRepository;
            _bookingReadRepository = bookingReadRepository;
            _providerRepository = providerRepository;
            _resourceResolver = resourceResolver;
            _serviceRepository = serviceRepository;
            _availabilityWriteRepository = availabilityWriteRepository;
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

            // Resolve the bookable resource the SAME way booking creation does: a
            // membership, the organization itself, or a legacy individual sub-provider.
            // This handler used to assume the third case unconditionally, which 404'd
            // every booking made against a member — i.e. every booking under the current
            // staff model. A caller-supplied NewStaffId is a genuine reassignment and
            // must be bookable; the resource already on the booking is not re-litigated,
            // so a member who has since been deactivated can still have their existing
            // appointments moved.
            var isStaffChange = request.NewStaffId is not null
                && request.NewStaffId.Value != existingBooking.StaffId;
            var newStaffId = request.NewStaffId ?? existingBooking.StaffId;

            var resource = await _resourceResolver.ResolveAsync(
                provider,
                newStaffId,
                requireBookable: isStaffChange,
                cancellationToken);

            // Validate booking constraints for the new time (status, business hours, holidays).
            var validationResult = await _availabilityService.ValidateBookingConstraintsAsync(
                provider,
                service,
                request.NewStartTime,
                cancellationToken);

            if (!validationResult.IsValid)
                throw new ConflictException($"Booking validation failed: {string.Join(", ", validationResult.Errors)}");

            // Does anything else already occupy the new time for this resource? Keyed by the
            // resolved resource id, mirroring creation — IsTimeSlotAvailableAsync cannot be
            // used here because its signature demands a staff Provider aggregate, which a
            // member does not have. The booking being moved is excluded: a shift within its
            // own buffer window would otherwise collide with itself.
            var newEndTime = request.NewStartTime.AddMinutes(service.Duration.Value + BufferMinutes);
            var conflicts = await _bookingReadRepository.GetConflictingBookingsAsync(
                resource.ResourceId,
                request.NewStartTime,
                newEndTime,
                cancellationToken);

            if (conflicts.Any(b => b.Id != existingBooking.Id))
                throw new ConflictException("The requested time slot is not available");

            // Reschedule the booking (returns new booking)
            var newBooking = existingBooking.Reschedule(
                request.NewStartTime,
                newStaffId,
                request.Reason);

            // Update existing booking (marked as rescheduled)
            await _bookingWriteRepository.UpdateBookingAsync(existingBooking, cancellationToken);

            // Save new booking
            await _bookingWriteRepository.SaveBookingAsync(newBooking, cancellationToken);

            // ========================================
            // ATOMIC AVAILABILITY SLOT MANAGEMENT
            // ========================================

            // Step 1: Release old availability slots
            await ReleaseOldAvailabilitySlotsAsync(
                resource.SlotOwnerId,
                existingBooking.TimeSlot.StartTime,
                existingBooking.TimeSlot.EndTime,
                existingBooking.Id.Value,
                cancellationToken);

            // Step 2: Mark new availability slots as booked
            await MarkNewAvailabilitySlotsAsBookedAsync(
                resource.SlotOwnerId,
                newBooking.TimeSlot.StartTime,
                newBooking.TimeSlot.EndTime,
                newBooking.Id.Value,
                cancellationToken);

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

        /// <summary>
        /// Release old availability slots back to Available status
        /// Finds all slots that overlap with the old booking time
        /// </summary>
        private async Task ReleaseOldAvailabilitySlotsAsync(
            ProviderId providerId,
            DateTime startTime,
            DateTime endTime,
            Guid bookingId,
            CancellationToken cancellationToken)
        {
            var date = DateOnly.FromDateTime(startTime);
            var startTimeOnly = TimeOnly.FromDateTime(startTime);
            var endTimeOnly = TimeOnly.FromDateTime(endTime);

            // Find all availability slots that overlap with the old booking
            var overlappingSlots = await _availabilityWriteRepository.FindOverlappingSlotsAsync(
                providerId,
                date.ToDateTime(TimeOnly.MinValue),
                startTimeOnly,
                endTimeOnly,
                null,
                cancellationToken);

            // Release slots that were booked by this booking
            foreach (var slot in overlappingSlots)
            {
                if (slot.Status == Domain.Enums.AvailabilityStatus.Booked &&
                    slot.BookingId == bookingId)
                {
                    // Release the slot back to Available
                    slot.Release("RescheduleBookingCommandHandler");
                    await _availabilityWriteRepository.UpdateAsync(slot, cancellationToken);

                    _logger.LogDebug(
                        "Released availability slot {SlotId} for old booking {BookingId}",
                        slot.Id,
                        bookingId);
                }
            }

            _logger.LogInformation(
                "Released {Count} availability slots for old booking {BookingId}",
                overlappingSlots.Count(s => s.BookingId == bookingId),
                bookingId);
        }

        /// <summary>
        /// Mark new availability slots as booked
        /// Finds all slots that overlap with the new booking time
        /// </summary>
        private async Task MarkNewAvailabilitySlotsAsBookedAsync(
            ProviderId providerId,
            DateTime startTime,
            DateTime endTime,
            Guid newBookingId,
            CancellationToken cancellationToken)
        {
            var date = DateOnly.FromDateTime(startTime);
            var startTimeOnly = TimeOnly.FromDateTime(startTime);
            var endTimeOnly = TimeOnly.FromDateTime(endTime);

            // Find all availability slots that overlap with the new booking
            var overlappingSlots = await _availabilityWriteRepository.FindOverlappingSlotsAsync(
                providerId,
                date.ToDateTime(TimeOnly.MinValue),
                startTimeOnly,
                endTimeOnly,
                null,
                cancellationToken);

            if (!overlappingSlots.Any())
            {
                _logger.LogWarning(
                    "No availability slots found for new booking time {StartTime} to {EndTime}. " +
                    "Provider may not have availability data seeded for this time range.",
                    startTime,
                    endTime);
                return;
            }

            // Mark slots as booked
            foreach (var slot in overlappingSlots)
            {
                if (slot.Status == Domain.Enums.AvailabilityStatus.Available ||
                    slot.Status == Domain.Enums.AvailabilityStatus.TentativeHold)
                {
                    slot.MarkAsBooked(newBookingId, "RescheduleBookingCommandHandler");
                    await _availabilityWriteRepository.UpdateAsync(slot, cancellationToken);

                    _logger.LogDebug(
                        "Marked availability slot {SlotId} as booked for new booking {BookingId}",
                        slot.Id,
                        newBookingId);
                }
                else if (slot.Status == Domain.Enums.AvailabilityStatus.Booked)
                {
                    // This should not happen if validation passed
                    throw new ConflictException(
                        $"Availability slot {slot.Id} is already booked. Concurrent booking conflict detected.");
                }
            }

            _logger.LogInformation(
                "Marked {Count} availability slots as booked for new booking {BookingId}",
                overlappingSlots.Count,
                newBookingId);
        }
    }
}
