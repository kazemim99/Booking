// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/PaymentReadRepository.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class PaymentReadRepository : EfReadRepositoryBase<Payment, PaymentId, ServiceCatalogDbContext>, IPaymentReadRepository
    {
        public PaymentReadRepository(ServiceCatalogDbContext context)
            : base(context)
        {
        }

        public new async Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Payment>> GetByCustomerIdAsync(
            UserId customerId,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Include(p => p.Transactions)
                .Where(p => p.CustomerId == customerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Payment>> GetByProviderIdAsync(
            ProviderId providerId,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Include(p => p.Transactions)
                .Where(p => p.ProviderId == providerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Payment>> GetByBookingIdAsync(
            BookingId bookingId,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Include(p => p.Transactions)
                .Where(p => p.BookingId == bookingId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Payment>> GetByStatusAsync(
            PaymentStatus status,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Include(p => p.Transactions)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Payment>> GetByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Include(p => p.Transactions)
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Payment>> GetProviderPaymentsInRangeAsync(
            ProviderId providerId,
            DateTime startDate,
            DateTime endDate,
            PaymentStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = DbSet
                .AsNoTracking()
                .Include(p => p.Transactions)
                .Where(p => p.ProviderId == providerId
                            && p.CreatedAt >= startDate
                            && p.CreatedAt <= endDate);

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            return await query
                .OrderBy(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
