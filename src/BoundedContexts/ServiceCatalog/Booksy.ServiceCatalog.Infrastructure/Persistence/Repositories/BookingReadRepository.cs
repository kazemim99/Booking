// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/BookingReadRepository.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
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
    }
}
