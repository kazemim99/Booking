// ========================================
// AsanRezerve.ServiceCatalog.Application/Services/BookingStaffNames.cs
// ========================================
using AsanRezerve.ServiceCatalog.Application.Abstractions.Identity;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using ProviderAggregate = AsanRezerve.ServiceCatalog.Domain.Aggregates.Provider;

namespace AsanRezerve.ServiceCatalog.Application.Services
{
    /// <summary>Who a booking is held against, by name, for the customer's own views of their bookings.</summary>
    public interface IBookingStaffNames
    {
        /// <summary>
        /// The name of the salon member <paramref name="staffId"/> points at, by <see cref="PersonName.ForMember"/>
        /// (real name, else the salon's name for them, else the salon's own name). Null when the booking is held
        /// by the salon itself, or the id is none of its members: then no team member is assigned.
        /// </summary>
        Task<string?> ForAsync(ProviderAggregate? salon, Guid staffId, CancellationToken cancellationToken = default);
    }

    /// <inheritdoc cref="IBookingStaffNames"/>
    /// <remarks>
    /// Scoped: a customer's page of bookings asks about the same salon many times, so each salon's roster is read
    /// once per request.
    /// </remarks>
    public sealed class BookingStaffNames : IBookingStaffNames
    {
        private readonly IOrganizationMembershipRepository _memberships;
        private readonly IPersonDirectory _people;
        private readonly Dictionary<Guid, IReadOnlyDictionary<Guid, string>> _bySalon = new();

        public BookingStaffNames(IOrganizationMembershipRepository memberships, IPersonDirectory people)
        {
            _memberships = memberships;
            _people = people;
        }

        public async Task<string?> ForAsync(
            ProviderAggregate? salon, Guid staffId, CancellationToken cancellationToken = default)
        {
            // The salon booked as a whole (a solo business) is not a team member.
            if (salon is null || staffId == Guid.Empty || staffId == salon.Id.Value)
                return null;

            if (!_bySalon.TryGetValue(salon.Id.Value, out var names))
            {
                names = await NamesOfAsync(salon, cancellationToken);
                _bySalon[salon.Id.Value] = names;
            }

            return names.TryGetValue(staffId, out var name) ? name : null;
        }

        private async Task<IReadOnlyDictionary<Guid, string>> NamesOfAsync(
            ProviderAggregate salon, CancellationToken cancellationToken)
        {
            // Every membership, terminated ones included: a past booking still names who did it.
            var members = await _memberships.GetByOrganizationAsync(salon.Id, cancellationToken);
            var personIds = members
                .Where(m => m.PersonId is not null)
                .Select(m => m.PersonId!.Value)
                .Distinct()
                .ToList();
            var people = personIds.Count > 0
                ? await _people.FindByIdsAsync(personIds, cancellationToken)
                : new Dictionary<Guid, PersonInfo>();

            return members.ToDictionary(
                m => m.Id,
                m =>
                {
                    PersonInfo? person = null;
                    if (m.PersonId is not null)
                        people.TryGetValue(m.PersonId.Value, out person);
                    return PersonName.ForMember(person, m.StaffProfile?.DisplayName, salon.Profile.BusinessName);
                });
        }
    }
}
