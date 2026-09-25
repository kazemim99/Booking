// ========================================
// AsanRezerve.ServiceCatalog.Application/Services/BookableResourceResolver.cs
// ========================================
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using ProviderAggregate = AsanRezerve.ServiceCatalog.Domain.Aggregates.Provider;

namespace AsanRezerve.ServiceCatalog.Application.Services
{
    /// <summary>What kind of thing a booking's <c>StaffId</c> points at.</summary>
    /// <remarks>
    /// There were three kinds until the provider hierarchy was removed; <c>LegacySubProvider</c>
    /// (an employee modelled as a second Provider) is gone along with the model itself, and the
    /// resolver has no branch that can produce it.
    /// </remarks>
    public enum BookableResourceKind
    {
        /// <summary>A person working at the organization (the current staff model).</summary>
        Member,

        /// <summary>The organization itself — a solo/direct booking with no specific staff member.</summary>
        Organization
    }

    /// <summary>
    /// A resolved bookable resource: what a booking is held against, and how its availability
    /// slots are keyed.
    /// </summary>
    /// <param name="ResourceId">The value stored in <c>Booking.StaffId</c>.</param>
    /// <param name="Kind">Which of the two resource kinds this is.</param>
    /// <param name="SlotOwnerId">The provider whose availability rows carry the slot.</param>
    /// <param name="SlotStaffId">
    /// The <c>ProviderAvailability.StaffId</c> to narrow slot lookups by: the membership id for a
    /// <see cref="BookableResourceKind.Member"/>, and null for
    /// <see cref="BookableResourceKind.Organization"/> — the salon booked as a whole owns all of
    /// its capacity, so its lookups stay unnarrowed.
    /// </param>
    /// <remarks>
    /// This field used to be deliberately absent, on the grounds that
    /// <c>FindOverlappingSlotsAsync</c> could not filter on <c>StaffId</c> and a key here would be
    /// "a lie in the type". The repository can filter now, so the key is real — and it had to
    /// become real, because without it booking one member marked every colleague's overlapping
    /// slot as Booked.
    /// </remarks>
    /// <param name="PersonId">
    /// The person behind this resource, when there is one. Null for
    /// <see cref="BookableResourceKind.Organization"/> and for a member whose record has not been
    /// claimed yet. Used to tell a provider-entered walk-in from a customer booking: if the caller
    /// *is* the resource, no request→confirm handshake is warranted.
    /// </param>
    public sealed record BookableResource(
        Guid ResourceId,
        BookableResourceKind Kind,
        ProviderId SlotOwnerId,
        UserId? PersonId,
        Guid? SlotStaffId);

    /// <summary>
    /// Resolves the bookable resource a booking is (or will be) held against.
    /// </summary>
    /// <remarks>
    /// This exists because booking creation and booking rescheduling both need the same answer, and
    /// historically only creation knew it: reschedule still assumed every staff reference was an
    /// individual sub-provider <c>Provider</c> row, so it returned 404 for any booking made against a
    /// membership — i.e. every booking made under the current staff model. Keeping the rule in one
    /// place is the point; see openspec/changes/fix-reschedule-membership-staff.
    /// </remarks>
    public interface IBookableResourceResolver
    {
        /// <summary>
        /// Resolves <paramref name="resourceId"/> against <paramref name="organization"/>.
        /// </summary>
        /// <param name="requireBookable">
        /// When true, a member that is not currently bookable (inactive, or not providing services) and a
        /// legacy sub-provider that is not Active are rejected. Creation requires this. Rescheduling an
        /// existing booking to a new time does not re-litigate it for the resource already on the booking.
        /// </param>
        /// <exception cref="NotFoundException">The id matches no membership, organization, or sub-provider.</exception>
        /// <exception cref="ConflictException">It resolves, but belongs elsewhere or is not bookable.</exception>
        Task<BookableResource> ResolveAsync(
            ProviderAggregate organization,
            Guid resourceId,
            bool requireBookable,
            CancellationToken cancellationToken = default);
    }

    /// <inheritdoc cref="IBookableResourceResolver"/>
    public sealed class BookableResourceResolver : IBookableResourceResolver
    {
        private readonly IOrganizationMembershipRepository _membershipRepository;
        private readonly IProviderReadRepository _providerRepository;

        public BookableResourceResolver(
            IOrganizationMembershipRepository membershipRepository,
            IProviderReadRepository providerRepository)
        {
            _membershipRepository = membershipRepository;
            _providerRepository = providerRepository;
        }

        public async Task<BookableResource> ResolveAsync(
            ProviderAggregate organization,
            Guid resourceId,
            bool requireBookable,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(organization);

            // The organization booked directly — no specific staff member.
            if (resourceId == organization.Id.Value)
            {
                return new BookableResource(
                    resourceId,
                    BookableResourceKind.Organization,
                    organization.Id,
                    PersonId: null,
                    // The salon as a whole, so its slot lookups are not narrowed to anyone.
                    SlotStaffId: null);
            }

            var membership = await _membershipRepository.GetByIdAsync(resourceId, cancellationToken);
            if (membership is not null)
            {
                if (membership.OrganizationId != organization.Id)
                    throw new ConflictException("Member does not belong to the specified organization");

                if (requireBookable &&
                    (membership.Status != MembershipStatus.Active || !membership.ProvidesServices))
                    throw new ConflictException("This team member is not currently bookable");

                // A member's slots belong to the organization but are keyed by their
                // membership id, which is what keeps one member's booking off a colleague.
                return new BookableResource(
                    resourceId,
                    BookableResourceKind.Member,
                    organization.Id,
                    PersonId: membership.PersonId,
                    SlotStaffId: resourceId);
            }

            // There is nothing else a staff reference can be. The third branch here used to
            // resolve a legacy Individual sub-provider (matched on ParentProviderId) — the
            // model where an employee was a second Provider. That is gone, so a resource id
            // is either this salon or one of its memberships.
            throw new NotFoundException($"Bookable resource with ID {resourceId} not found");
        }
    }
}
