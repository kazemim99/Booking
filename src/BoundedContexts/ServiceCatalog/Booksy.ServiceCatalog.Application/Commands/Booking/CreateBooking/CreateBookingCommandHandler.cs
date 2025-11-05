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
    /// Handler for creating a new booking request
    /// </summary>
    public sealed class CreateBookingCommandHandler : ICommandHandler<CreateBookingCommand, CreateBookingResult>
    {
        private readonly IBookingWriteRepository _bookingWriteRepository;
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly IAvailabilityService _availabilityService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateBookingCommandHandler> _logger;

        public CreateBookingCommandHandler(
            IBookingWriteRepository bookingWriteRepository,
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            IAvailabilityService availabilityService,
            IUnitOfWork unitOfWork,
            ILogger<CreateBookingCommandHandler> logger)
        {
            _bookingWriteRepository = bookingWriteRepository;
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _availabilityService = availabilityService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CreateBookingResult> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Creating booking for Customer {CustomerId}, Service {ServiceId}, Staff {StaffId} at {StartTime}",
                request.CustomerId, request.ServiceId, request.StaffId, request.StartTime);

            // Load provider with all necessary data
            var provider = await _providerRepository.GetByIdAsync(
                ProviderId.From(request.ProviderId),
                cancellationToken);

            if (provider == null)
                throw new NotFoundException($"Provider with ID {request.ProviderId} not found");

            // Load service
            var service = await _serviceRepository.GetByIdAsync(
                ServiceId.Create(request.ServiceId),
                cancellationToken);

            if (service == null)
                throw new NotFoundException($"Service with ID {request.ServiceId} not found");

            // Verify service belongs to provider
            if (service.ProviderId != provider.Id)
                throw new ConflictException("Service does not belong to the specified provider");

            // Get staff member
            var staff = provider.Staff.FirstOrDefault(s => s.Id == request.StaffId);
            if (staff == null)
                throw new NotFoundException($"Staff member with ID {request.StaffId} not found");

            // Validate availability
            var isAvailable = await _availabilityService.IsTimeSlotAvailableAsync(
                provider,
                service,
                staff,
                request.StartTime,
                service.Duration,
                cancellationToken);

            if (!isAvailable)
                throw new ConflictException("The requested time slot is not available");

            // Validate booking constraints
            var validationResult = await _availabilityService.ValidateBookingConstraintsAsync(
                provider,
                service,
                request.StartTime,
                cancellationToken);

            if (!validationResult.IsValid)
                throw new ConflictException($"Booking validation failed: {string.Join(", ", validationResult.Errors)}");

            // Get booking policy from service or use default
            var bookingPolicy = service.BookingPolicy ?? BookingPolicy.Default;

            // Create the booking
            var booking = Domain.Aggregates.BookingAggregate.Booking.CreateBookingRequest(
                customerId: UserId.From(request.CustomerId),
                providerId: provider.Id,
                serviceId: service.Id,
                staffId: staff.Id,
                startTime: request.StartTime,
                duration: service.Duration,
                totalPrice: service.BasePrice,
                policy: bookingPolicy,
                customerNotes: request.CustomerNotes);

            // Save booking
            await _bookingWriteRepository.SaveBookingAsync(booking, cancellationToken);

            // Commit transaction and publish domain events
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation("Booking {BookingId} created successfully", booking.Id);

            // Return result
            return new CreateBookingResult(
                BookingId: booking.Id.Value,
                CustomerId: booking.CustomerId.Value,
                ProviderId: booking.ProviderId.Value,
                ServiceId: booking.ServiceId.Value,
                StaffId: booking.StaffId,
                StartTime: booking.TimeSlot.StartTime,
                EndTime: booking.TimeSlot.EndTime,
                TotalPrice: booking.TotalPrice.Amount,
                DepositAmount: booking.PaymentInfo.DepositAmount.Amount,
                RequiresDeposit: booking.Policy.RequireDeposit,
                Status: booking.Status.ToString(),
                RequestedAt: booking.RequestedAt);
        }
    }
}
