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

        public async Task<Payment?> GetByAuthorityAsync(string authority, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.Authority == authority, cancellationToken);
        }

        public async Task<IReadOnlyList<Payment>> GetFailedPaymentsAsync(
            DateTime fromDate,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Include(p => p.Transactions)
                .Where(p => p.Status == PaymentStatus.Failed && p.FailedAt >= fromDate)
                .OrderByDescending(p => p.FailedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<Payment> Payments, int TotalCount)> GetCustomerPaymentHistoryAsync(
            UserId customerId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int skip = 0,
            int take = 20,
            CancellationToken cancellationToken = default)
        {
            var query = DbSet
                .AsNoTracking()
                .Include(p => p.Transactions)
                .Where(p => p.CustomerId == customerId);

            if (startDate.HasValue)
                query = query.Where(p => p.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.CreatedAt <= endDate.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (payments, totalCount);
        }

        public async Task<(decimal TotalRevenue, decimal TotalRefunds, int SuccessfulPayments, int TotalPayments)> GetProviderRevenueStatsAsync(
            ProviderId providerId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            var payments = await DbSet
                .AsNoTracking()
                .Where(p => p.ProviderId == providerId
                            && p.CreatedAt >= startDate
                            && p.CreatedAt <= endDate)
                .ToListAsync(cancellationToken);

            var totalPayments = payments.Count;
            var successfulPayments = payments.Count(p => p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.PartiallyRefunded);
            var totalRevenue = payments
                .Where(p => p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.PartiallyRefunded)
                .Sum(p => p.PaidAmount.Amount);
            var totalRefunds = payments
                .Sum(p => p.RefundedAmount.Amount);

            return (totalRevenue, totalRefunds, successfulPayments, totalPayments);
        }

        public async Task<IReadOnlyList<Payment>> GetPaymentsForReconciliationAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Include(p => p.Transactions)
                .Where(p => p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.PartiallyRefunded || p.Status == PaymentStatus.Refunded)
                .Where(p => p.CapturedAt >= startDate && p.CapturedAt <= endDate)
                .OrderBy(p => p.CapturedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
