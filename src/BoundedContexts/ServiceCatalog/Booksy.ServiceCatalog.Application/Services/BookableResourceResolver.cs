// ========================================
// Booksy.ServiceCatalog.Application/Services/BookableResourceResolver.cs
// ========================================
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using ProviderAggregate = Booksy.ServiceCatalog.Domain.Aggregates.Provider;

namespace Booksy.ServiceCatalog.Application.Services
{
    /// <summary>What kind of thing a booking's <c>StaffId</c> points at.</summary>
    public enum BookableResourceKind
    {
        /// <summary>A person working at the organization (the current staff model).</summary>
        Member,

        /// <summary>The organization itself — a solo/direct booking with no specific staff member.</summary>
        Organization,

        /// <summary>An individual sub-provider, from before staff became memberships.</summary>
        LegacySubProvider
    }

    /// <summary>
    /// A resolved bookable resource: what a booking is held against, and how its availability
    /// slots are keyed.
    /// </summary>
    /// <param name="ResourceId">The value stored in <c>Booking.StaffId</c>.</param>
    /// <param name="Kind">Which of the three resource kinds this is.</param>
    /// <param name="SlotOwnerId">The provider whose availability rows carry the slot.</param>
    /// <remarks>
    /// There is deliberately no "staff key" here. A member's availability rows hang off the
    /// organization and carry <c>ProviderAvailability.StaffId</c>, but
    /// <c>FindOverlappingSlotsAsync</c> does not filter on it — so slot lookups cannot currently be
    /// narrowed to one member, and pretending otherwise would be a lie in the type. Closing that gap
    /// belongs to the booking-slot-integrity change.
    /// </remarks>
    /// <param name="PersonId">
    /// The person behind this resource, when there is one — a member's account, or a legacy
    /// sub-provider's owner. Null for <see cref="BookableResourceKind.Organization"/> and for a member
    /// whose record has not been claimed yet. Used to tell a provider-entered walk-in from a customer
    /// booking: if the caller *is* the resource, no request→confirm handshake is warranted.
    /// </param>
    public sealed record BookableResource(
        Guid ResourceId,
        BookableResourceKind Kind,
        ProviderId SlotOwnerId,
        UserId? PersonId);

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
                    PersonId: null);
            }

            var membership = await _membershipRepository.GetByIdAsync(resourceId, cancellationToken);
            if (membership is not null)
            {
                if (membership.OrganizationId != organization.Id)
                    throw new ConflictException("Member does not belong to the specified organization");

                if (requireBookable &&
                    (membership.Status != MembershipStatus.Active || !membership.ProvidesServices))
                    throw new ConflictException("This team member is not currently bookable");

                // A member's slots belong to the organization, not to a sub-provider row.
                return new BookableResource(
                    resourceId,
                    BookableResourceKind.Member,
                    organization.Id,
                    PersonId: membership.PersonId);
            }

            var legacyStaffProvider = await _providerRepository.GetByIdAsync(
                ProviderId.From(resourceId), cancellationToken);

            if (legacyStaffProvider is null)
                throw new NotFoundException($"Bookable resource with ID {resourceId} not found");

            if (legacyStaffProvider.ParentProviderId != organization.Id)
                throw new ConflictException("Staff provider does not belong to the specified organization");

            if (requireBookable && legacyStaffProvider.Status != ProviderStatus.Active)
                throw new ConflictException(
                    $"Staff provider {DescribeLegacyStaff(legacyStaffProvider)} is not currently active");

            // A legacy sub-provider owns its own availability rows.
            return new BookableResource(
                resourceId,
                BookableResourceKind.LegacySubProvider,
                legacyStaffProvider.Id,
                PersonId: legacyStaffProvider.OwnerId);
        }

        private static string DescribeLegacyStaff(ProviderAggregate staff)
        {
            var name = $"{staff.OwnerFirstName} {staff.OwnerLastName}".Trim();
            return string.IsNullOrEmpty(name) ? staff.Profile.BusinessName : name;
        }
    }
}
