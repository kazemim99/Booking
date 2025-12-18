// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/CreateBooking/CreateBookingCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.DomainServices;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.CreateBooking
{
    /// <summary>
    /// Handler for creating a new booking request with atomic availability slot locking
    /// Prevents double-booking through Serializable transaction isolation
    /// </summary>
    public sealed class CreateBookingCommandHandler : ICommandHandler<CreateBookingCommand, CreateBookingResult>
    {
        private readonly IBookingWriteRepository _bookingWriteRepository;
        private readonly IBookingReadRepository _bookingReadRepository;
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly IProviderAvailabilityWriteRepository _availabilityWriteRepository;
        private readonly IAvailabilityService _availabilityService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateBookingCommandHandler> _logger;

        public CreateBookingCommandHandler(
            IBookingWriteRepository bookingWriteRepository,
            IBookingReadRepository bookingReadRepository,
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            IProviderAvailabilityWriteRepository availabilityWriteRepository,
            IAvailabilityService availabilityService,
            IUnitOfWork unitOfWork,
            ILogger<CreateBookingCommandHandler> logger)
        {
            _bookingWriteRepository = bookingWriteRepository;
            _bookingReadRepository = bookingReadRepository;
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _availabilityWriteRepository = availabilityWriteRepository;
            _availabilityService = availabilityService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CreateBookingResult> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Creating booking for Customer {CustomerId}, Service {ServiceId}, StaffProvider {StaffProviderId} at {StartTime}",
                request.CustomerId, request.ServiceId, request.StaffProviderId, request.StartTime);

            // Load organization provider
            var provider = await _providerRepository.GetByIdAsync(
                ProviderId.From(request.ProviderId),
                cancellationToken);

            if (provider == null)
                throw new NotFoundException($"Provider with ID {request.ProviderId} not found");

            // Load service
            var service = await _serviceRepository.GetByIdAsync(
                ServiceId.From(request.ServiceId),
                cancellationToken);

            if (service == null)
                throw new NotFoundException($"Service with ID {request.ServiceId} not found");

            // Verify service belongs to provider
            if (service.ProviderId != provider.Id)
                throw new ConflictException("Service does not belong to the specified provider");

            // Load staff provider (individual provider in hierarchy)
            var staffProvider = await _providerRepository.GetByIdAsync(
                ProviderId.From(request.StaffProviderId),
                cancellationToken);

            if (staffProvider == null)
                throw new NotFoundException($"Staff provider with ID {request.StaffProviderId} not found");

            // Verify staff provider belongs to organization hierarchy
            if (staffProvider.ParentProviderId != provider.Id)
                throw new ConflictException("Staff provider does not belong to the specified organization");

            // Check if staff provider is active
            if (staffProvider.Status != Domain.Enums.ProviderStatus.Active)
            {
                var staffName = $"{staffProvider.OwnerFirstName} {staffProvider.OwnerLastName}".Trim();
                if (string.IsNullOrEmpty(staffName))
                    staffName = staffProvider.Profile.BusinessName;
                throw new ConflictException($"Staff provider {staffName} is not currently active");
            }

            // Validate booking constraints (provider status, business hours, holidays, etc.)
            var validationResult = await _availabilityService.ValidateBookingConstraintsAsync(
                provider,
                service,
                request.StartTime,
                cancellationToken);

            if (!validationResult.IsValid)
                throw new ConflictException($"Booking validation failed: {string.Join(", ", validationResult.Errors)}");

            // Check for booking conflicts with existing appointments
            var bookingEndTime = request.StartTime.AddMinutes(service.Duration.Value + 15); // Add 15-min buffer
            var conflictingBookings = await _bookingReadRepository.GetConflictingBookingsAsync(
                staffProvider.Id.Value,
                request.StartTime,
                bookingEndTime,
                cancellationToken);

            if (conflictingBookings.Any())
                throw new ConflictException("This time slot conflicts with an existing booking");

            // Get booking policy from service or use default
            var bookingPolicy = service.BookingPolicy ?? BookingPolicy.Default;

            // Create the booking
            var booking = Domain.Aggregates.BookingAggregate.Booking.CreateBookingRequest(
                customerId: UserId.From(request.CustomerId),
                providerId: provider.Id,
                serviceId: service.Id,
                staffId: staffProvider.Id.Value,
                startTime: request.StartTime,
                duration: service.Duration,
                totalPrice: service.BasePrice,
                policy: bookingPolicy,
                customerNotes: request.CustomerNotes);

            // Save booking
            await _bookingWriteRepository.SaveBookingAsync(booking, cancellationToken);

            // Mark availability slot as booked atomically
            var endTime = request.StartTime.Add(service.Duration.ToTimeSpan());
            await MarkAvailabilityAsBookedAsync(
                staffProvider.Id,
                request.StartTime,
                endTime,
                booking.Id.Value,
                cancellationToken);


            _logger.LogInformation("Booking {BookingId} created successfully and availability slot marked as booked", booking.Id);

            // Return result
            return new CreateBookingResult(
                BookingId: booking.Id.Value,
                CustomerId: booking.CustomerId.Value,
                ProviderId: booking.ProviderId.Value,
                ServiceId: booking.ServiceId.Value,
                StaffProviderId: booking.StaffId,
                StartTime: booking.TimeSlot.StartTime,
                EndTime: booking.TimeSlot.EndTime,
                TotalPrice: booking.TotalPrice.Amount,
                DepositAmount: booking.PaymentInfo.DepositAmount.Amount,
                RequiresDeposit: booking.Policy.RequireDeposit,
                Status: booking.Status.ToString(),
                RequestedAt: booking.RequestedAt);
        }

        /// <summary>
        /// Marks availability slots as booked for the booking time range
        /// Handles multi-slot bookings (bookings spanning multiple 30-min slots)
        /// </summary>
        private async Task MarkAvailabilityAsBookedAsync(
            ProviderId providerId,
            DateTime startTime,
            DateTime endTime,
            Guid bookingId,
            CancellationToken cancellationToken)
        {
            var date = startTime.Date;
            var startTimeOnly = TimeOnly.FromDateTime(startTime);
            var endTimeOnly = TimeOnly.FromDateTime(endTime);

            // Find all overlapping availability slots
            var overlappingSlots = await _availabilityWriteRepository.FindOverlappingSlotsAsync(
                providerId,
                date,
                startTimeOnly,
                endTimeOnly,
                null,
                cancellationToken);

            if (overlappingSlots.Count == 0)
            {
                _logger.LogWarning(
                    "No availability slots found for Provider {ProviderId} on {Date} from {StartTime} to {EndTime}. " +
                    "Booking will be created but availability calendar won't reflect it.",
                    providerId.Value,
                    date,
                    startTimeOnly,
                    endTimeOnly);
                return;
            }

            // Mark all overlapping slots as booked
            foreach (var slot in overlappingSlots)
            {
                if (slot.Status == Domain.Enums.AvailabilityStatus.Available ||
                    slot.Status == Domain.Enums.AvailabilityStatus.TentativeHold)
                {
                    slot.MarkAsBooked(bookingId, "CreateBookingCommandHandler");
                    await _availabilityWriteRepository.UpdateAsync(slot, cancellationToken);

                    _logger.LogDebug(
                        "Marked availability slot {SlotId} as booked for booking {BookingId}",
                        slot.Id,
                        bookingId);
                }
                else if (slot.Status == Domain.Enums.AvailabilityStatus.Booked)
                {
                    // Slot already booked - this is a race condition that should be prevented by Serializable isolation
                    _logger.LogError(
                        "Race condition detected: Availability slot {SlotId} is already booked. " +
                        "This should not happen with Serializable isolation.",
                        slot.Id);

                    throw new ConflictException(
                        "The selected time slot has just been booked by another customer. " +
                        "Please select a different time.");
                }
            }

            _logger.LogInformation(
                "Marked {Count} availability slots as booked for booking {BookingId}",
                overlappingSlots.Count,
                bookingId);
        }
    }
}
