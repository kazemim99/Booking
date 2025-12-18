// ========================================
// Booksy.ServiceCatalog.Domain/Repositories/IBookingReadRepository.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Models;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Repositories
{
    /// <summary>
    /// Read repository for Booking aggregate - optimized for queries
    /// </summary>
    public interface IBookingReadRepository : IReadRepository<Booking, BookingId>
    {
        /// <summary>
        /// Get bookings by customer ID
        /// </summary>
        Task<IReadOnlyList<Booking>> GetByCustomerIdAsync(UserId customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get bookings by provider ID
        /// </summary>
        Task<IReadOnlyList<Booking>> GetByProviderIdAsync(ProviderId providerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get bookings by service ID
        /// </summary>
        Task<IReadOnlyList<Booking>> GetByServiceIdAsync(ServiceId serviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get bookings by staff ID
        /// </summary>
        Task<IReadOnlyList<Booking>> GetByStaffIdAsync(Guid staffId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get bookings by status
        /// </summary>
        Task<IReadOnlyList<Booking>> GetByStatusAsync(BookingStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get bookings for a provider within a date range
        /// </summary>
        Task<IReadOnlyList<Booking>> GetProviderBookingsInDateRangeAsync(
            ProviderId providerId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get bookings for a staff member within a date range
        /// </summary>
        Task<IReadOnlyList<Booking>> GetStaffBookingsInDateRangeAsync(
            Guid staffId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get upcoming bookings for a customer
        /// </summary>
        Task<IReadOnlyList<Booking>> GetUpcomingBookingsForCustomerAsync(
            UserId customerId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get upcoming bookings for a provider
        /// </summary>
        Task<IReadOnlyList<Booking>> GetUpcomingBookingsForProviderAsync(
            ProviderId providerId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a time slot is available for a staff member
        /// </summary>
        Task<bool> IsTimeSlotAvailableAsync(
            Guid staffId,
            DateTime startTime,
            DateTime endTime,
            BookingId? excludeBookingId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get conflicting bookings for a time slot
        /// </summary>
        Task<IReadOnlyList<Booking>> GetConflictingBookingsAsync(
            Guid staffId,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get booking count by customer for analytics
        /// </summary>
        Task<int> GetBookingCountByCustomerAsync(UserId customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get booking count by provider for analytics
        /// </summary>
        Task<int> GetBookingCountByProviderAsync(ProviderId providerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get completed bookings count for a customer
        /// </summary>
        Task<int> GetCompletedBookingsCountAsync(UserId customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get no-show count for a customer
        /// </summary>
        Task<int> GetNoShowCountAsync(UserId customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get customer's booking history with pagination
        /// </summary>
        Task<PagedResult<Booking>> GetCustomerBookingHistoryAsync(
            UserId customerId,
            PaginationRequest pagination,
            BookingStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get provider's booking history with pagination
        /// </summary>
        Task<PagedResult<Booking>> GetProviderBookingHistoryAsync(
            ProviderId providerId,
            PaginationRequest pagination,
            BookingStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Search bookings with multiple filter criteria and pagination
        /// </summary>
        Task<(IReadOnlyList<Booking> Bookings, int TotalCount)> SearchBookingsAsync(
            Guid? providerId = null,
            Guid? customerId = null,
            Guid? serviceId = null,
            Guid? staffId = null,
            BookingStatus? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get booking statistics for a provider within a date range
        /// </summary>
        Task<BookingStatistics> GetStatisticsAsync(
            Guid providerId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default);
    }
}
