// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/BookingReadRepository.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class BookingReadRepository : EfReadRepositoryBase<Booking, BookingId, ServiceCatalogDbContext>, IBookingReadRepository
    {
        public BookingReadRepository(
            ServiceCatalogDbContext context,
            ILogger<BookingReadRepository> logger)
            : base(context)
        {
        }

        public override async Task<Booking?> GetByIdAsync(BookingId id, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetByCustomerIdAsync(UserId customerId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.TimeSlot.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetByProviderIdAsync(ProviderId providerId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(b => b.ProviderId == providerId)
                .OrderByDescending(b => b.TimeSlot.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetByServiceIdAsync(ServiceId serviceId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(b => b.ServiceId == serviceId)
                .OrderByDescending(b => b.TimeSlot.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetByStaffIdAsync(Guid staffId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(b => b.StaffId == staffId)
                .OrderByDescending(b => b.TimeSlot.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetByStatusAsync(BookingStatus status, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(b => b.Status == status)
                .OrderByDescending(b => b.TimeSlot.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetProviderBookingsInDateRangeAsync(
            ProviderId providerId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(b => b.ProviderId == providerId &&
                           b.TimeSlot.StartTime >= startDate &&
                           b.TimeSlot.StartTime < endDate)
                .OrderBy(b => b.TimeSlot.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetStaffBookingsInDateRangeAsync(
            Guid staffId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(b => b.StaffId == staffId &&
                           b.TimeSlot.StartTime >= startDate &&
                           b.TimeSlot.StartTime < endDate)
                .OrderBy(b => b.TimeSlot.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetUpcomingBookingsForCustomerAsync(
            UserId customerId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await DbSet
                .Where(b => b.CustomerId == customerId &&
                           b.TimeSlot.StartTime > now &&
                           (b.Status == BookingStatus.Requested || b.Status == BookingStatus.Confirmed))
                .OrderBy(b => b.TimeSlot.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetUpcomingBookingsForProviderAsync(
            ProviderId providerId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await DbSet
                .Where(b => b.ProviderId == providerId &&
                           b.TimeSlot.StartTime > now &&
                           (b.Status == BookingStatus.Requested || b.Status == BookingStatus.Confirmed))
                .OrderBy(b => b.TimeSlot.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsTimeSlotAvailableAsync(
            Guid staffId,
            DateTime startTime,
            DateTime endTime,
            BookingId? excludeBookingId = null,
            CancellationToken cancellationToken = default)
        {
            var query = DbSet
                .Where(b => b.StaffId == staffId &&
                           (b.Status == BookingStatus.Requested || b.Status == BookingStatus.Confirmed) &&
                           b.TimeSlot.StartTime < endTime &&
                           b.TimeSlot.EndTime > startTime);

            if (excludeBookingId != null)
            {
                query = query.Where(b => b.Id != excludeBookingId);
            }

            var conflictingBookings = await query.CountAsync(cancellationToken);
            return conflictingBookings == 0;
        }

        public async Task<IReadOnlyList<Booking>> GetConflictingBookingsAsync(
            Guid staffId,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Where(b => b.StaffId == staffId &&
                           (b.Status == BookingStatus.Requested || b.Status == BookingStatus.Confirmed) &&
                           b.TimeSlot.StartTime < endTime &&
                           b.TimeSlot.EndTime > startTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetBookingCountByCustomerAsync(UserId customerId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .CountAsync(b => b.CustomerId == customerId, cancellationToken);
        }

        public async Task<int> GetBookingCountByProviderAsync(ProviderId providerId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .CountAsync(b => b.ProviderId == providerId, cancellationToken);
        }

        public async Task<int> GetCompletedBookingsCountAsync(UserId customerId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .CountAsync(b => b.CustomerId == customerId && b.Status == BookingStatus.Completed, cancellationToken);
        }

        public async Task<int> GetNoShowCountAsync(UserId customerId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .CountAsync(b => b.CustomerId == customerId && b.Status == BookingStatus.NoShow, cancellationToken);
        }

        public async Task<PagedResult<Booking>> GetCustomerBookingHistoryAsync(
            UserId customerId,
            PaginationRequest pagination,
            BookingStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = DbSet.Where(b => b.CustomerId == customerId);

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            query = query.OrderByDescending(b => b.TimeSlot.StartTime);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Booking>(
                items,
                totalCount,
                pagination.PageNumber,
                pagination.PageSize);
        }

        public async Task<PagedResult<Booking>> GetProviderBookingHistoryAsync(
            ProviderId providerId,
            PaginationRequest pagination,
            BookingStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = DbSet.Where(b => b.ProviderId == providerId);

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(b => b.TimeSlot.StartTime >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(b => b.TimeSlot.StartTime < toDate.Value);
            }

            query = query.OrderByDescending(b => b.TimeSlot.StartTime);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Booking>(
                items,
                totalCount,
                pagination.PageNumber,
                pagination.PageSize);
        }

        public async Task<(IReadOnlyList<Booking> Bookings, int TotalCount)> SearchBookingsAsync(
            Guid? providerId = null,
            Guid? customerId = null,
            Guid? serviceId = null,
            Guid? staffId = null,
            BookingStatus? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var query = DbSet.AsQueryable();

            // Apply filters
            if (providerId.HasValue)
            {
                var providerIdValue = ProviderId.From(providerId.Value);
                query = query.Where(b => b.ProviderId == providerIdValue);
            }

            if (customerId.HasValue)
            {
                var customerIdValue = UserId.From(customerId.Value);
                query = query.Where(b => b.CustomerId == customerIdValue);
            }

            if (serviceId.HasValue)
            {
                var serviceIdValue = ServiceId.From(serviceId.Value);
                query = query.Where(b => b.ServiceId == serviceIdValue);
            }

            if (staffId.HasValue)
            {
                query = query.Where(b => b.StaffId == staffId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(b => b.TimeSlot.StartTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(b => b.TimeSlot.StartTime < endDate.Value);
            }

            // Order by most recent first
            query = query.OrderByDescending(b => b.TimeSlot.StartTime);

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var bookings = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (bookings, totalCount);
        }

        public async Task<Domain.Models.BookingStatistics> GetStatisticsAsync(
            Guid providerId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            var providerIdValue = ProviderId.From(providerId);
            var query = DbSet.Where(b => b.ProviderId == providerIdValue);

            if (startDate.HasValue)
            {
                query = query.Where(b => b.TimeSlot.StartTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(b => b.TimeSlot.StartTime < endDate.Value);
            }

            var bookings = await query.ToListAsync(cancellationToken);

            // Calculate statistics
            var totalBookings = bookings.Count;
            var requestedBookings = bookings.Count(b => b.Status == BookingStatus.Requested);
            var confirmedBookings = bookings.Count(b => b.Status == BookingStatus.Confirmed);
            var completedBookings = bookings.Count(b => b.Status == BookingStatus.Completed);
            var cancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled);
            var noShowBookings = bookings.Count(b => b.Status == BookingStatus.NoShow);
            var rescheduledBookings = bookings.Count(b => b.Status == BookingStatus.Rescheduled);

            // Calculate revenue (assuming first currency or default)
            var currency = bookings.FirstOrDefault()?.TotalPrice.Currency ?? "USD";
            var totalRevenue = bookings.Sum(b => b.TotalPrice.Amount);
            var completedRevenue = bookings
                .Where(b => b.Status == BookingStatus.Completed)
                .Sum(b => b.PaymentInfo.PaidAmount.Amount);
            var pendingRevenue = bookings
                .Where(b => b.Status == BookingStatus.Requested || b.Status == BookingStatus.Confirmed)
                .Sum(b => b.TotalPrice.Amount - b.PaymentInfo.PaidAmount.Amount);
            var refundedAmount = bookings
                .Where(b => b.PaymentInfo.RefundedAmount != null)
                .Sum(b => b.PaymentInfo.RefundedAmount?.Amount ?? 0);

            // Calculate rates
            var totalCompletableBookings = confirmedBookings + completedBookings + noShowBookings;
            var completionRate = totalCompletableBookings > 0
                ? (double)completedBookings / totalCompletableBookings * 100
                : 0;
            var noShowRate = totalCompletableBookings > 0
                ? (double)noShowBookings / totalCompletableBookings * 100
                : 0;
            var cancellationRate = totalBookings > 0
                ? (double)cancelledBookings / totalBookings * 100
                : 0;

            return new Domain.Models.BookingStatistics(
                totalBookings: totalBookings,
                requestedBookings: requestedBookings,
                confirmedBookings: confirmedBookings,
                completedBookings: completedBookings,
                cancelledBookings: cancelledBookings,
                noShowBookings: noShowBookings,
                rescheduledBookings: rescheduledBookings,
                totalRevenue: totalRevenue,
                completedRevenue: completedRevenue,
                pendingRevenue: pendingRevenue,
                refundedAmount: refundedAmount,
                currency: currency,
                completionRate: Math.Round(completionRate, 2),
                noShowRate: Math.Round(noShowRate, 2),
                cancellationRate: Math.Round(cancellationRate, 2),
                startDate: startDate,
                endDate: endDate);
        }
    }
}
