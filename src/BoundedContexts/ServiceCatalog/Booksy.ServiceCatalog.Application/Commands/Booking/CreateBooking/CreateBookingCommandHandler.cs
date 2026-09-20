// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/CreateBooking/CreateBookingCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Core.Application.Services.Notifications;
using Booksy.ServiceCatalog.Application.Services;
using Booksy.ServiceCatalog.Application.Services.Notifications;
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
        private readonly IProviderCustomerRepository _providerCustomers;
        private readonly ISmsNotificationService _sms;
        private readonly INotificationRaiser _notifications;
        private readonly IBookingReminderScheduler _reminders;
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
            IProviderCustomerRepository providerCustomers,
            ISmsNotificationService sms,
            INotificationRaiser notifications,
            IBookingReminderScheduler reminders,
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
            _providerCustomers = providerCustomers;
            _sms = sms;
            _notifications = notifications;
            _reminders = reminders;
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

            // A start time in the past is a malformed request, not a scheduling conflict: no state
            // of the salon could make it bookable. It used to fall through to the constraint check
            // below, whose failures are all reported as 409 Conflict, so asking for yesterday got
            // the same answer as asking for a slot someone else had taken.
            if (request.StartTime <= DateTime.UtcNow)
                throw new DomainValidationException(
                    nameof(request.StartTime), "Cannot create a booking in the past");

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

            // Who the salon is booking for. A salon-entered booking always names its customer:
            // picked from the book, or typed in — and typing them in puts them in the book, so the
            // same person is one entry with one number however they were booked.
            Domain.Aggregates.ProviderCustomer? bookedFor = null;
            if (isProviderCreated)
            {
                bookedFor = await ResolveProviderCustomerAsync(provider.Id, request, cancellationToken);
                booking.RecordForProviderCustomer(bookedFor.Id, request.NotifyCustomer);
            }
            else if (request.ProviderCustomerId is not null)
            {
                throw new ForbiddenException("Only the salon can book for a customer in its customer book");
            }

            // Save booking
            await _bookingWriteRepository.SaveBookingAsync(booking, cancellationToken);

            // Mark availability slot as booked atomically
            var endTime = request.StartTime.Add(totalDuration.ToTimeSpan());
            await MarkAvailabilityAsBookedAsync(
                // How slots are keyed is the resolver's decision, not this handler's:
                // member slots belong to the organization and carry StaffId=MembershipId,
                // while the organization booked directly owns all of its slots.
                resource.SlotOwnerId,
                resource.SlotStaffId,
                request.StartTime,
                endTime,
                booking.Id.Value,
                cancellationToken);


            // Tell the people this booking concerns. Recorded on this unit of work, so the notifications
            // commit with the booking or vanish with it; nothing is sent from inside the request.
            await RaiseBookingNotificationsAsync(booking, provider, bookedFor is not null, cancellationToken);

            // The customer hears about the appointment the salon just made for them. Last, after
            // every check and write has succeeded, and never fatal: the appointment is real whether
            // or not the message gets out.
            if (bookedFor is not null && request.NotifyCustomer)
            {
                await NotifyCustomerAsync(bookedFor, provider, service, request.StartTime, booking.Id.Value, cancellationToken);
            }

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
        /// Records the notifications a new booking causes: one for the customer, one for the salon.
        /// </summary>
        /// <remarks>
        /// <para>A salon-entered booking and a customer's own booking are different events and get different
        /// codes. An online booking arrives as a request awaiting the salon's decision; a salon-entered one is
        /// already confirmed.</para>
        ///
        /// <para><b>A walk-in customer is not notified here.</b> When the salon books for someone in its own
        /// customer book, the aggregate's customer is the salon owner — notifying them would tell the salon
        /// about its own work. That person is told by the Persian SMS below, on their own phone.</para>
        /// </remarks>
        private async Task RaiseBookingNotificationsAsync(
            Domain.Aggregates.BookingAggregate.Booking booking,
            Domain.Aggregates.Provider provider,
            bool isForProviderCustomer,
            CancellationToken cancellationToken)
        {
            var confirmed = booking.Status == Domain.Enums.BookingStatus.Confirmed;
            var parameters = new Dictionary<string, string>
            {
                [NotificationParameter.BusinessName] = provider.Profile.BusinessName,
                [NotificationParameter.StartTime] = booking.TimeSlot.StartTime.ToString("o"),
            };
        
            if (!isForProviderCustomer)
            {
                await _notifications.RaiseAsync(
                    confirmed
                        ? Domain.Enums.NotificationEventCode.BookingConfirmed
                        : Domain.Enums.NotificationEventCode.BookingRequested,
                    booking.CustomerId.Value,
                    dedupKey: booking.Id.Value,
                    parameters: parameters,
                    subjectType: BookingReminderScheduler.BookingSubject,
                    subjectId: booking.Id.Value,
                    cancellationToken: cancellationToken);
            }
        
            // The salon is a person too: notifications are addressed to the owner's user id, never to a
            // provider id, because the inbox and preferences are keyed by user.
            await _notifications.RaiseAsync(
                confirmed
                    ? Domain.Enums.NotificationEventCode.NewBookingConfirmed
                    : Domain.Enums.NotificationEventCode.NewBookingRequest,
                provider.OwnerId.Value,
                dedupKey: booking.Id.Value,
                parameters: parameters,
                subjectType: BookingReminderScheduler.BookingSubject,
                subjectId: booking.Id.Value,
                cancellationToken: cancellationToken);
        
            // Reminders only make sense once the appointment is actually happening.
            if (confirmed)
                await _reminders.ScheduleAsync(booking, cancellationToken);
        }

        private async Task NotifyCustomerAsync(
            Domain.Aggregates.ProviderCustomer customer,
            ProviderAggregate provider,
            Domain.Aggregates.Service service,
            DateTime startTime,
            Guid bookingId,
            CancellationToken cancellationToken)
        {
            try
            {
                var (success, _, error) = await _sms.SendSmsAsync(
                    customer.PhoneNumber.Value,
                    BookingSmsText.Confirmed(
                        customer.FirstName, provider.Profile.BusinessName, service.Name, startTime),
                    new Dictionary<string, object> { ["bookingId"] = bookingId },
                    cancellationToken);

                if (!success)
                    _logger.LogWarning("Booking SMS to {BookingId}'s customer failed: {Error}", bookingId, error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send the booking SMS for booking {BookingId}", bookingId);
            }
        }

        /// <summary>
        /// The customer-book entry a salon-entered booking is for: the one picked from the book, or
        /// the one the number belongs to — saved now if the salon has never booked them before.
        /// A saved name is never overwritten by what was typed this time.
        /// </summary>
        private async Task<Domain.Aggregates.ProviderCustomer> ResolveProviderCustomerAsync(
            ProviderId providerId,
            CreateBookingCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ProviderCustomerId is { } pickedId)
            {
                return await _providerCustomers.GetAsync(providerId, pickedId, cancellationToken)
                    ?? throw new NotFoundException("مشتری در فهرست مشتریان این سالن پیدا نشد");
            }

            if (string.IsNullOrWhiteSpace(request.WalkInPhone))
                throw new DomainValidationException("WalkInPhone", "شماره موبایل مشتری الزامی است");

            Core.Domain.ValueObjects.PhoneNumber phone;
            try
            {
                phone = Core.Domain.ValueObjects.PhoneNumber.From(request.WalkInPhone!);
            }
            catch (ArgumentException)
            {
                throw new DomainValidationException("WalkInPhone", "شماره موبایل مشتری معتبر نیست");
            }

            var existing = await _providerCustomers.GetByPhoneAsync(providerId, phone, cancellationToken);
            if (existing != null)
                return existing;

            if (string.IsNullOrWhiteSpace(request.WalkInFirstName))
                throw new DomainValidationException("WalkInFirstName", "نام مشتری الزامی است");

            var customer = Domain.Aggregates.ProviderCustomer.Create(
                providerId,
                request.WalkInFirstName!,
                request.WalkInLastName,
                phone,
                notes: null,
                Domain.Enums.CustomerSource.Booking);
            await _providerCustomers.AddAsync(customer, cancellationToken);
            return customer;
        }

        /// <summary>
        /// Marks availability slots as booked for the booking time range
        /// Handles multi-slot bookings (bookings spanning multiple 30-min slots)
        /// </summary>
        private async Task MarkAvailabilityAsBookedAsync(
            ProviderId providerId,
            Guid? slotStaffId,
            DateTime startTime,
            DateTime endTime,
            Guid bookingId,
            CancellationToken cancellationToken)
        {
            var date = startTime.Date;
            var startTimeOnly = TimeOnly.FromDateTime(startTime);
            var endTimeOnly = TimeOnly.FromDateTime(endTime);

            // Scoped to the member being booked. Unscoped, this marked EVERY colleague's
            // overlapping slot as Booked too, so one 10:00 appointment consumed the whole
            // salon's 10:00 capacity. (A staffId was once passed as the 5th argument, which
            // is `excludeSlotId` — a no-op that read like staff-scoping and hid the gap.)
            // Null here means the organization was booked directly, which does own all of
            // its capacity, so that path stays unnarrowed.
            var overlappingSlots = await _availabilityWriteRepository.FindOverlappingSlotsAsync(
                providerId,
                date,
                startTimeOnly,
                endTimeOnly,
                excludeSlotId: null,
                staffId: slotStaffId,
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
