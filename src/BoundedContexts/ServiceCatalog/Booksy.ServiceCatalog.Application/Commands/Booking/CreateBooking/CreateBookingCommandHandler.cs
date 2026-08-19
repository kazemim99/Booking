// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/CreateBooking/CreateBookingCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate.Entities;
using Booksy.ServiceCatalog.Domain.DomainServices;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using ProviderAggregate = Booksy.ServiceCatalog.Domain.Aggregates.Provider;

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
        private readonly IBookableResourceResolver _resourceResolver;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly IProviderAvailabilityWriteRepository _availabilityWriteRepository;
        private readonly IAvailabilityService _availabilityService;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<CreateBookingCommandHandler> _logger;

        public CreateBookingCommandHandler(
            IBookingWriteRepository bookingWriteRepository,
            IBookingReadRepository bookingReadRepository,
            IProviderReadRepository providerRepository,
            IBookableResourceResolver resourceResolver,
            IServiceReadRepository serviceRepository,
            IProviderAvailabilityWriteRepository availabilityWriteRepository,
            IAvailabilityService availabilityService,
            IServiceCatalogUnitOfWork unitOfWork,
            ILogger<CreateBookingCommandHandler> logger)
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

            // Load every service in the visit. ServiceIds supersedes the
            // single ServiceId (kept for caller compatibility); duplicates are
            // collapsed, order preserved (first = the booking's primary).
            var requestedServiceIds =
                (request.ServiceIds is { Count: > 0 }
                    ? request.ServiceIds
                    : new[] { request.ServiceId })
                .Distinct()
                .ToList();

            var services = new List<Domain.Aggregates.Service>();
            foreach (var id in requestedServiceIds)
            {
                var loaded = await _serviceRepository.GetByIdAsync(
                    ServiceId.From(id),
                    cancellationToken);

                if (loaded == null)
                    throw new NotFoundException($"Service with ID {id} not found");

                if (loaded.ProviderId != provider.Id)
                    throw new ConflictException(
                        "Service does not belong to the specified provider");

                if (services.Count > 0 &&
                    loaded.BasePrice.Currency != services[0].BasePrice.Currency)
                    throw new ConflictException(
                        "All services in one booking must share a currency");

                services.Add(loaded);
            }

            var service = services[0];

            // The visit occupies the combined length and costs the combined
            // price of its services (performed back-to-back).
            var totalDuration = Duration.FromMinutes(
                services.Sum(x => x.Duration.Value));
            var combinedPrice = Price.Create(
                services.Sum(x => x.BasePrice.Amount),
                service.BasePrice.Currency);
            var lineItems = services
                .Select(x => new BookingServiceItem(
                    x.Id.Value,
                    x.Name,
                    x.BasePrice.Amount,
                    x.BasePrice.Currency,
                    (int)x.Duration.Value))
                .ToList();

            // Resolve the bookable resource. StaffProviderId is a RESOURCE id: a
            // MembershipId (a person working here), the organization itself (solo
            // direct booking), or a legacy individual sub-provider until migrated.
            // Rescheduling resolves the same way, via the same resolver.
            var resource = await _resourceResolver.ResolveAsync(
                provider, request.StaffProviderId, requireBookable: true, cancellationToken);
            var resourceId = resource.ResourceId;

            // Validate booking constraints (provider status, business hours, holidays, etc.)
            var validationResult = await _availabilityService.ValidateBookingConstraintsAsync(
                provider,
                service,
                request.StartTime,
                cancellationToken);

            if (!validationResult.IsValid)
                throw new ConflictException($"Booking validation failed: {string.Join(", ", validationResult.Errors)}");

            // Check for booking conflicts with existing appointments
            var bookingEndTime = request.StartTime.AddMinutes(totalDuration.Value + 15); // Add 15-min buffer
            var conflictingBookings = await _bookingReadRepository.GetConflictingBookingsAsync(
                resourceId,
                request.StartTime,
                bookingEndTime,
                cancellationToken);

            if (conflictingBookings.Any())
                throw new ConflictException("This time slot conflicts with an existing booking");

            // Resolve the *effective* booking policy: a service-level override wins, otherwise the provider's own
            // default, otherwise the platform default (which requires no deposit). The booking snapshots whichever
            // policy applies, so a later policy change never alters an existing booking.
            var bookingPolicy = service.BookingPolicy ?? provider.BookingPolicy ?? BookingPolicy.Default;

            // Provider-entered walk-ins are born Confirmed: when the caller
            // owns the organization (or is the staff member being booked),
            // the request→confirm handshake would be them approving
            // themselves. Detected server-side from the JWT identity — a
            // client-supplied flag could be spoofed by customers.
            var callerId = UserId.From(request.CustomerId);
            var isProviderCreated =
                provider.OwnerId == callerId
                || (resource.PersonId is not null && resource.PersonId.Equals(callerId));

            // Create the booking
            var booking = isProviderCreated
                ? Domain.Aggregates.BookingAggregate.Booking.CreateConfirmedByProvider(
                    customerId: callerId,
                    providerId: provider.Id,
                    serviceId: service.Id,
                    staffId: resourceId,
                    startTime: request.StartTime,
                    duration: totalDuration,
                    totalPrice: combinedPrice,
                    policy: bookingPolicy,
                    customerNotes: request.CustomerNotes,
                    services: lineItems)
                : Domain.Aggregates.BookingAggregate.Booking.CreateBookingRequest(
                    customerId: callerId,
                    providerId: provider.Id,
                    serviceId: service.Id,
                    staffId: resourceId,
                    startTime: request.StartTime,
                    duration: totalDuration,
                    totalPrice: combinedPrice,
                    policy: bookingPolicy,
                    customerNotes: request.CustomerNotes,
                    services: lineItems);

            // Save booking
            await _bookingWriteRepository.SaveBookingAsync(booking, cancellationToken);

            // Mark availability slot as booked atomically
            var endTime = request.StartTime.Add(totalDuration.ToTimeSpan());
            await MarkAvailabilityAsBookedAsync(
                // How slots are keyed is the resolver's decision, not this handler's:
                // member slots belong to the organization and carry StaffId=MembershipId;
                // legacy sub-provider slots are keyed by the sub-provider itself.
                resource.SlotOwnerId,
                request.StartTime,
                endTime,
                booking.Id.Value,
                cancellationToken);


            _logger.LogInformation("Booking {BookingId} created successfully and availability slot marked as booked", booking.Id);
            Telemetry.BookingMetrics.BookingCreated();

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
            // NOTE: the 5th argument is `excludeSlotId` — a slot to skip — NOT a staff
            // filter. `staffId` used to be passed here, which is a no-op in practice (a
            // membership id never equals a slot id) but reads as staff-scoping that is not
            // happening: FindOverlappingSlotsAsync does not filter on ProviderAvailability
            // .StaffId at all, so an organization's overlapping slots are returned
            // regardless of which member they belong to. Left as-is behaviourally; the
            // missing staff filter belongs to the booking-slot-integrity change.
            var overlappingSlots = await _availabilityWriteRepository.FindOverlappingSlotsAsync(
                providerId,
                date,
                startTimeOnly,
                endTimeOnly,
                excludeSlotId: null,
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
