// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetCustomerBookings/GetCustomerBookingsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetCustomerBookings
{
    /// <summary>
    /// Handler for getting paginated customer bookings with enriched data
    /// Follows the established pattern of using repository pagination methods
    /// </summary>
    public sealed class GetCustomerBookingsQueryHandler : IQueryHandler<GetCustomerBookingsQuery, PagedResult<CustomerBookingDto>>
    {
        private readonly IBookingReadRepository _bookingRepository;
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ILogger<GetCustomerBookingsQueryHandler> _logger;

        public GetCustomerBookingsQueryHandler(
            IBookingReadRepository bookingRepository,
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            ILogger<GetCustomerBookingsQueryHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        public async Task<PagedResult<CustomerBookingDto>> Handle(GetCustomerBookingsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Getting bookings for customer {CustomerId} with filters: Status={Status}, From={From}, To={To}, Page={Page}, PageSize={PageSize}",
                request.CustomerId, request.Status, request.FromDate, request.ToDate, request.Pagination.PageNumber, request.Pagination.PageSize);

            // Parse status if provided
            BookingStatus? status = null;
            if (!string.IsNullOrEmpty(request.Status))
            {
                if (Enum.TryParse<BookingStatus>(request.Status, true, out var parsedStatus))
                {
                    status = parsedStatus;
                }
                else
                {
                    _logger.LogWarning("Invalid booking status provided: {Status}", request.Status);
                }
            }

            // Get bookings from repository with pagination and filters
            var pagedResult = await _bookingRepository.GetCustomerBookingHistoryAsync(
                UserId.From(request.CustomerId.ToString()),
                request.Pagination,
                status,
                request.FromDate,
                request.ToDate,
                cancellationToken);

            // Map to enriched DTOs with provider, service, and staff details
            // NOTE: This creates N+1 queries. For better performance, consider:
            // 1. Adding a specification-based method that includes related entities
            // 2. Using a database view or stored procedure
            // 3. Batching the queries for providers and services
            var enrichedDtos = new List<CustomerBookingDto>();

            foreach (var booking in pagedResult.Items)
            {
                // Load provider and service for additional details
                var provider = await _providerRepository.GetByIdAsync(booking.ProviderId, cancellationToken);
                var service = await _serviceRepository.GetByIdAsync(booking.ServiceId, cancellationToken);
                var staff = provider?.Staff.FirstOrDefault(s => s.Id == booking.StaffId);

                enrichedDtos.Add(new CustomerBookingDto(
                    BookingId: booking.Id.Value,
                    CustomerId: booking.CustomerId.Value,
                    ProviderId: booking.ProviderId.Value,
                    ServiceId: booking.ServiceId.Value,
                    StaffId: booking.StaffId,
                    ServiceName: service?.Name ?? "Unknown Service",
                    ProviderName: provider?.Profile.BusinessName ?? "Unknown Provider",
                    StaffName: staff?.FullName,
                    StartTime: booking.TimeSlot.StartTime,
                    EndTime: booking.TimeSlot.EndTime,
                    DurationMinutes: booking.Duration.Value,
                    Status: booking.Status.ToString(),
                    TotalPrice: booking.TotalPrice.Amount,
                    Currency: booking.TotalPrice.Currency,
                    PaymentStatus: booking.PaymentInfo.Status.ToString(),
                    RequestedAt: booking.RequestedAt,
                    ConfirmedAt: booking.ConfirmedAt,
                    CustomerNotes: booking.CustomerNotes));
            }

            _logger.LogInformation(
                "Found {Count} bookings for customer {CustomerId} (Page {Page} of {TotalPages})",
                pagedResult.TotalCount, request.CustomerId, pagedResult.PageNumber, pagedResult.TotalPages);

            // Return a new PagedResult with the enriched DTOs
            return new PagedResult<CustomerBookingDto>(
                enrichedDtos,
                pagedResult.TotalCount,
                pagedResult.PageNumber,
                pagedResult.PageSize);
        }
    }
}
