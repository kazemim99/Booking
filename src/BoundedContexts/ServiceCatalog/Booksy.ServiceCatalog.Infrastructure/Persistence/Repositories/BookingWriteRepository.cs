// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/BookingWriteRepository.cs
// ========================================
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class BookingWriteRepository : EfWriteRepositoryBase<Booking, BookingId, ServiceCatalogDbContext>, IBookingWriteRepository
    {
        public BookingWriteRepository(
            ServiceCatalogDbContext context,
            ILogger<BookingWriteRepository> logger)
            : base(context)
        {
        }

        public new async Task<Booking?> GetByIdAsync(BookingId id, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task SaveBookingAsync(Booking booking, CancellationToken cancellationToken = default)
        {
            await DbSet.AddAsync(booking, cancellationToken);
        }

        public async Task UpdateBookingAsync(Booking booking, CancellationToken cancellationToken = default)
        {
            // The aggregate is normally loaded tracked (GetByIdAsync): change
            // tracking already detects modifications and marks newly added
            // children as Added. Calling Context.Update() on a tracked graph
            // forces EVERY reachable entity to Modified — including brand-new
            // BookingHistoryEntry rows (client-generated Guid keys), which then
            // execute as UPDATEs affecting 0 rows and throw
            // DbUpdateConcurrencyException on every booking mutation.
            if (Context.Entry(booking).State == EntityState.Detached)
            {
                Context.Update(booking);
            }

            // History is an append-only, immutable child collection. New
            // entries carry client-generated Guid keys, so EF's navigation
            // discovery assumes they are EXISTING rows and marks them
            // Modified — the resulting UPDATE matches 0 rows and every
            // booking mutation throws DbUpdateConcurrencyException. Existing
            // entries are only ever tracked Unchanged (never edited), so any
            // Modified history entry is necessarily a new one.
            Context.ChangeTracker.DetectChanges();
            foreach (var entry in Context.ChangeTracker.Entries<BookingHistoryEntry>())
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.State = EntityState.Added;
                }
            }

            await Task.CompletedTask;
        }

        public async Task DeleteBookingAsync(Booking booking, CancellationToken cancellationToken = default)
        {
            DbSet.Remove(booking);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(BookingId id, CancellationToken cancellationToken = default)
        {
            return await DbSet.AnyAsync(b => b.Id == id, cancellationToken);
        }
    }
}
