using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;

namespace Booksy.ServiceCatalog.Application.Services.Interfaces;

/// <summary>
/// Makes a service-providing member a bookable resource.
///
/// A member is bookable when two things exist: they are qualified for the
/// organization's services, and they have availability. Both are keyed by
/// <c>MembershipId</c> — the membership IS the bookable resource identity, so no
/// shadow "staff provider" record is ever created (that anti-pattern is what the
/// identity redesign removed).
///
/// Availability rows belong to the organization (<c>ProviderId = org</c>) and
/// identify the person via <c>StaffId = MembershipId</c>.
/// </summary>
public interface IMemberBookabilityService
{
    /// <summary>
    /// Brings the member's bookability in step with their membership. Idempotent:
    /// safe to call on every activation/role change. A member who does not provide
    /// services (or is not active) is a no-op.
    /// Changes are tracked; the caller commits them.
    /// </summary>
    /// <param name="regenerateAvailability">
    /// Pass true when the member's WORKING SCHEDULE changed. Existing days are
    /// otherwise left alone for idempotency, which would keep serving the old roster;
    /// this first clears the member's still-free future slots so the new schedule
    /// takes effect. Booked and held slots are never removed.
    /// </param>
    Task<MemberBookabilityResult> SyncAsync(
        OrganizationMembership membership,
        bool regenerateAvailability = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The mirror of <see cref="SyncAsync"/>, from the service's side: qualifies the
    /// organization's service-providing members for a service and activates it once at
    /// least one of them can perform it.
    /// </summary>
    /// <remarks>
    /// Without this a newly added service is stranded: <c>Service.Create</c> leaves it in
    /// Draft with no qualified staff, <c>Activate()</c> refuses to run without a qualified
    /// member, and only membership events ever qualified anyone — so a salon could add a
    /// service and never be able to sell it.
    ///
    /// Members who have narrowed their assignments and do not list this service are left
    /// out, which is what makes assignments mean anything for a service added later.
    /// Changes are tracked; the caller commits them.
    /// </remarks>
    /// <returns>How many members were qualified.</returns>
    Task<int> SyncServiceAsync(
        Domain.Aggregates.Service service,
        CancellationToken cancellationToken = default);
}

public sealed record MemberBookabilityResult(int ServicesQualified, int SlotsGenerated)
{
    public static readonly MemberBookabilityResult None = new(0, 0);
}
