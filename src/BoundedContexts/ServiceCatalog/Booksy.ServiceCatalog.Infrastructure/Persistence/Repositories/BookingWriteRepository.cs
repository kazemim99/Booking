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
            Context.Update(booking);
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
