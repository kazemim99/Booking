using AsanRezerve.UserManagement.Domain.ReadModels;
using AsanRezerve.UserManagement.Domain.Repositories;
using AsanRezerve.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AsanRezerve.UserManagement.Infrastructure.Persistence.Repositories
{
    /// <inheritdoc cref="ICustomerBookingHistoryReadRepository"/>
    public sealed class CustomerBookingHistoryReadRepository : ICustomerBookingHistoryReadRepository
    {
        /// <summary>A cancelled booking is history, never "upcoming", whatever its start time.</summary>
        private const string CancelledStatus = "Cancelled";

        private readonly UserManagementDbContext _context;

        public CustomerBookingHistoryReadRepository(UserManagementDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<CustomerBookingHistoryEntry>> GetUpcomingAsync(
            Guid customerId,
            DateTime salonNow,
            int limit,
            CancellationToken cancellationToken = default)
        {
            return await _context.CustomerBookingHistory
                .AsNoTracking()
                .Where(e => e.CustomerId == customerId
                            && e.StartTime >= salonNow
                            && e.Status != CancelledStatus)
                .OrderBy(e => e.StartTime)
                .Take(Math.Max(1, limit))
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<CustomerBookingHistoryEntry> Items, int TotalCount)> GetHistoryPageAsync(
            Guid customerId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.CustomerBookingHistory
                .AsNoTracking()
                .Where(e => e.CustomerId == customerId);

            var total = await query.CountAsync(cancellationToken);
            var safePage = Math.Max(1, page);
            var safeSize = Math.Clamp(pageSize, 1, 100);

            var items = await query
                .OrderByDescending(e => e.StartTime)
                .Skip((safePage - 1) * safeSize)
                .Take(safeSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }
    }
}
