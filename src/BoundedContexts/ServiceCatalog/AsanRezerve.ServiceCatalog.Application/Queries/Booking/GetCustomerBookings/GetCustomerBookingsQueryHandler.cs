// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Booking/GetCustomerBookings/GetCustomerBookingsQueryHandler.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.DTOs;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Identity;
using AsanRezerve.ServiceCatalog.Application.Services;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Booking.GetCustomerBookings
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
        private readonly IBookingCustomer _bookingCustomer;
        private readonly IReviewReadRepository _reviews;
        private readonly IBookingStaffNames _staffNames;
        private readonly ILogger<GetCustomerBookingsQueryHandler> _logger;

        public GetCustomerBookingsQueryHandler(
            IBookingReadRepository bookingRepository,
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            IBookingCustomer bookingCustomer,
            IReviewReadRepository reviews,
            IBookingStaffNames staffNames,
            ILogger<GetCustomerBookingsQueryHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _bookingCustomer = bookingCustomer;
            _reviews = reviews;
            _staffNames = staffNames;
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

            // Get bookings from repository with pagination and filters — theirs, and the ones a
            // salon entered for their number.
            var theirEntries = await _bookingCustomer.TheirBookEntriesAsync(request.CustomerId, cancellationToken);
            var pagedResult = await _bookingRepository.GetCustomerBookingHistoryAsync(
                UserId.From(request.CustomerId.ToString()),
                request.Pagination,
                status,
                request.FromDate,
                request.ToDate,
                theirEntries,
                cancellationToken);

            // Map to enriched DTOs with provider, service, and staff details
            // NOTE: This creates N+1 queries. For better performance, consider:
            // 1. Adding a specification-based method that includes related entities
            // 2. Using a database view or stored procedure
            // 3. Batching the queries for providers and services
            var enrichedDtos = new List<CustomerBookingDto>();

            // Where each booking's review stands, for the person it is for. The list also holds the walk-ins a salon
            // owner entered for their clients (stored under the owner's id): those are not the owner's to review, so
            // they say nothing. One review per salon: each booking carries this person's review of its salon, from
            // whichever visit. One query for the page.
            var reviews = await _reviews.GetStatesByProviderIdsAsync(
                UserId.From(request.CustomerId),
                pagedResult.Items.Select(b => b.ProviderId).ToList(),
                cancellationToken);
            var now = DateTime.UtcNow;

            foreach (var booking in pagedResult.Items)
            {
                var review = BookingCustomer.IsFor(booking, request.CustomerId, theirEntries)
                    ? BookingReviewStanding.Of(booking, reviews.GetValueOrDefault(booking.ProviderId.Value), now)
                    : BookingReviewStanding.None;

                // Load provider and service for additional details
                var provider = await _providerRepository.GetByIdAsync(booking.ProviderId, cancellationToken);
                var service = await _serviceRepository.GetByIdAsync(booking.ServiceId, cancellationToken);

                enrichedDtos.Add(new CustomerBookingDto(
                    BookingId: booking.Id.Value,
                    CustomerId: booking.CustomerId.Value,
                    ProviderId: booking.ProviderId.Value,
                    ServiceId: booking.ServiceId.Value,
                    StaffId: booking.StaffId,
                    ServiceName: service?.Name ?? "Unknown Service",
                    ProviderName: provider?.Profile.BusinessName ?? "Unknown Provider",
                    StartTime: booking.TimeSlot.StartTime,
                    EndTime: booking.TimeSlot.EndTime,
                    DurationMinutes: booking.Duration.Value,
                    Status: booking.Status.ToString(),
                    TotalPrice: booking.TotalPrice.Amount,
                    Currency: booking.TotalPrice.Currency,
                    PaymentStatus: booking.PaymentInfo.Status.ToString(),
                    RequestedAt: booking.RequestedAt,
                    ConfirmedAt: booking.ConfirmedAt,
                    CustomerNotes: booking.CustomerNotes,
                    StaffName: await _staffNames.ForAsync(provider, booking.StaffId, cancellationToken),
                    RescheduleBlockedReason: booking.RescheduleBlockedReason(),
                    CanReview: review.CanReview,
                    ReviewBlockedReason: review.ReviewBlockedReason,
                    ReviewId: review.ReviewId,
                    ReviewStatus: review.ReviewStatus,
                    ReviewEditable: review.ReviewEditable,
                    ReviewBookingId: review.ReviewBookingId,
                    Subtotal: booking.SubtotalAmount,
                    DiscountAmount: booking.DiscountAmount ?? 0m,
                    DiscountTitle: booking.DiscountTitle,
                    DiscountCode: booking.DiscountCode));
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
