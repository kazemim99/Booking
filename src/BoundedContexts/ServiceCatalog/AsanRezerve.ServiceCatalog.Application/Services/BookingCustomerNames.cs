using AsanRezerve.ServiceCatalog.Application.Abstractions.Identity;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.Repositories;

namespace AsanRezerve.ServiceCatalog.Application.Services
{
    /// <summary>
    /// Who each of a salon's bookings is for, by name — the one rule the notifications already follow
    /// (<c>BookingNotificationParameters</c>): the salon's own client-book name for a walk-in, the customer's own real
    /// name otherwise, and NOTHING when there is only a placeholder or a phone. The salon's booking list carried no
    /// name at all, so its request card read «بدون نام» for «ناصر عابدی» (QA 2026-09-24).
    /// </summary>
    public interface IBookingCustomerNames
    {
        /// <summary>Booking id → the customer's real name. Bookings with no real name are absent.</summary>
        Task<IReadOnlyDictionary<Guid, string>> ForAsync(
            Guid providerId,
            IReadOnlyCollection<Booking> bookings,
            CancellationToken cancellationToken = default);
    }

    /// <inheritdoc />
    public sealed class BookingCustomerNames : IBookingCustomerNames
    {
        private readonly IProviderCustomerRepository _providerCustomers;
        private readonly IPersonDirectory _people;

        public BookingCustomerNames(IProviderCustomerRepository providerCustomers, IPersonDirectory people)
        {
            _providerCustomers = providerCustomers;
            _people = people;
        }

        public async Task<IReadOnlyDictionary<Guid, string>> ForAsync(
            Guid providerId,
            IReadOnlyCollection<Booking> bookings,
            CancellationToken cancellationToken = default)
        {
            var names = new Dictionary<Guid, string>();
            if (bookings.Count == 0)
                return names;

            var walkIns = bookings.Where(b => b.ProviderCustomerId is not null).ToList();
            if (walkIns.Count > 0)
            {
                var book = (await _providerCustomers.ListAsync(
                        Domain.ValueObjects.ProviderId.From(providerId), search: null, cancellationToken))
                    .ToDictionary(c => c.Id);

                foreach (var b in walkIns)
                {
                    if (book.TryGetValue(b.ProviderCustomerId!.Value, out var entry)
                        && PersonName.RealOrNull(entry.FirstName, entry.LastName) is { } name)
                        names[b.Id.Value] = name;
                }
            }

            var own = bookings.Where(b => b.ProviderCustomerId is null).ToList();
            if (own.Count > 0)
            {
                var people = await _people.FindByIdsAsync(
                    own.Select(b => b.CustomerId.Value).Distinct().ToList(), cancellationToken);

                foreach (var b in own)
                {
                    if (people.TryGetValue(b.CustomerId.Value, out var person)
                        && PersonName.RealOrNull(person.FirstName, person.LastName) is { } name)
                        names[b.Id.Value] = name;
                }
            }

            return names;
        }
    }
}
