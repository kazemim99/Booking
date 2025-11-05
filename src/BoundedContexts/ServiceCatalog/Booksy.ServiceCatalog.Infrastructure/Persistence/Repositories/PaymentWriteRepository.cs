// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/PaymentWriteRepository.cs
// ========================================
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class PaymentWriteRepository : EfWriteRepositoryBase<Payment, PaymentId, ServiceCatalogDbContext>, IPaymentWriteRepository
    {
        public PaymentWriteRepository(ServiceCatalogDbContext context)
            : base(context)
        {
        }

        public new async Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Payment?> GetByBookingIdAsync(BookingId bookingId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.BookingId == bookingId, cancellationToken);
        }

        public async Task<Payment?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId, cancellationToken);
        }

        public new async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            await DbSet.AddAsync(payment, cancellationToken);
        }

        public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            Context.Update(payment);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(PaymentId id, CancellationToken cancellationToken = default)
        {
            return await DbSet.AnyAsync(p => p.Id == id, cancellationToken);
        }
    }
}
