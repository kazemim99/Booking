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
    Task<MemberBookabilityResult> SyncAsync(
        OrganizationMembership membership,
        CancellationToken cancellationToken = default);
}

public sealed record MemberBookabilityResult(int ServicesQualified, int SlotsGenerated)
{
    public static readonly MemberBookabilityResult None = new(0, 0);
}
