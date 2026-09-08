using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Membership.CanManageOrganization;

/// <summary>
/// Whether the authenticated person may act on an organization, and in what capacity.
/// </summary>
/// <remarks>
/// The controllers used to answer this by asking "does this person OWN this provider?" —
/// three separate copies of the same owner-only check. Under the membership model that is
/// wrong in both directions: an employed manager or receptionist can legitimately run a
/// salon they do not own, and ownership is just one of the roles a membership can carry.
/// A staff member who owns nothing was refused (403) on every provider-scoped route,
/// which is what left the provider app blank for anyone but a salon owner.
/// </remarks>
public sealed record CanManageOrganizationQuery(
    Guid OrganizationId,
    OrganizationPermission Permission) : IQuery<bool>;

/// <summary>What the caller is trying to do, because not every member may do everything.</summary>
public enum OrganizationPermission
{
    /// <summary>
    /// Change the business itself — its profile, hours, services, team and settings.
    /// Owners and managers only: this is running the salon, not working in it.
    /// </summary>
    ManageOrganization = 0,

    /// <summary>
    /// See and act on the salon's appointments. Open to anyone with an active
    /// membership: the day book is the shared operational record of the people working
    /// that day, and a stylist who cannot see it cannot do their job.
    /// </summary>
    /// <remarks>
    /// This is salon-scoped, never cross-salon — a membership is still required. Narrowing
    /// a plain <c>StaffProvider</c> to their own column is a product decision, not a
    /// security one, and is left for when the app has a per-member calendar view.
    /// </remarks>
    ManageBookings = 1
}
