using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class ProviderCustomerRepository : IProviderCustomerRepository
    {
        private readonly ServiceCatalogDbContext _context;

        public ProviderCustomerRepository(ServiceCatalogDbContext context) => _context = context;

        private IQueryable<ProviderCustomer> Of(ProviderId providerId) =>
            _context.ProviderCustomers.Where(c => c.ProviderId == providerId);

        public Task<ProviderCustomer?> GetAsync(ProviderId providerId, Guid customerId, CancellationToken cancellationToken = default) =>
            Of(providerId).FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        public Task<ProviderCustomer?> GetByPhoneAsync(ProviderId providerId, PhoneNumber phoneNumber, CancellationToken cancellationToken = default) =>
            Of(providerId).FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber, cancellationToken);

        public async Task<IReadOnlyList<ProviderCustomer>> ListAsync(ProviderId providerId, string? search, CancellationToken cancellationToken = default)
        {
            // A salon's book is hundreds of rows, not millions: filtering in memory keeps the name and
            // number matching (Persian letters, any phone spelling) in one readable place.
            var all = await Of(providerId).AsNoTracking().ToListAsync(cancellationToken);
            var matches = string.IsNullOrWhiteSpace(search) ? all : all.Where(c => Matches(c, search.Trim()));
            return matches.OrderBy(c => c.FirstName).ThenBy(c => c.LastName).ToList();
        }

        public async Task<IReadOnlySet<string>> ExistingPhonesAsync(ProviderId providerId, IReadOnlyCollection<string> phoneValues, CancellationToken cancellationToken = default)
        {
            var saved = await Of(providerId).AsNoTracking().Select(c => c.PhoneNumber).ToListAsync(cancellationToken);
            var savedValues = saved.Select(p => p.Value).ToHashSet();
            return phoneValues.Where(savedValues.Contains).ToHashSet();
        }

        public async Task AddAsync(ProviderCustomer customer, CancellationToken cancellationToken = default) =>
            await _context.ProviderCustomers.AddAsync(customer, cancellationToken);

        public void Remove(ProviderCustomer customer) => _context.ProviderCustomers.Remove(customer);

        public async Task<IReadOnlyDictionary<Guid, ProviderCustomerBookingStats>> BookingStatsAsync(
            ProviderId providerId, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            var rows = await _context.Bookings.AsNoTracking()
                .Where(b => b.ProviderId == providerId && b.ProviderCustomerId != null && b.Status != BookingStatus.Cancelled)
                .Select(b => new { CustomerId = b.ProviderCustomerId!.Value, b.TimeSlot.StartTime, b.Status })
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(r => r.CustomerId)
                .ToDictionary(
                    g => g.Key,
                    g => new ProviderCustomerBookingStats(
                        g.Count(),
                        g.Count(r => r.StartTime >= nowUtc && r.Status is BookingStatus.Requested or BookingStatus.Confirmed),
                        g.Max(r => (DateTime?)r.StartTime)));
        }

        /// <summary>A name fragment, or a number fragment in any spelling ("0935...", "935...", "+98935...").</summary>
        private static bool Matches(ProviderCustomer customer, string search)
        {
            if (customer.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

            var digits = new string(search.Where(char.IsDigit).ToArray());
            if (digits.Length < 3)
                return false;
            if (digits.StartsWith('0'))
                digits = digits[1..];
            return customer.PhoneNumber.Value.Contains(digits, StringComparison.Ordinal);
        }
    }
}
