namespace AsanRezerve.UserManagement.Application.Services.Interfaces;

/// <summary>
/// Service for querying a person's organization memberships from ServiceCatalog, so a
/// token issued by UserManagement can carry them (refactor-identity-and-membership §5.4).
/// </summary>
public interface IMembershipInfoService
{
    /// <summary>
    /// All non-terminated memberships the given person holds, across every organization.
    /// </summary>
    Task<IReadOnlyList<MembershipSummary>> GetMembershipsForPersonAsync(
        Guid personId, CancellationToken cancellationToken = default);
}

/// <summary>
/// A membership as seen from UserManagement, resolved through <see cref="IMembershipInfoService"/>.
/// </summary>
/// <param name="Roles">
/// Comma-separated role names exactly as ServiceCatalog persists them (e.g. "Owner,StaffProvider").
/// Carried as a string on purpose: the MembershipRole enum belongs to ServiceCatalog's domain and
/// UserManagement does not (and should not) reference it.
/// </param>
public record MembershipSummary(
    Guid MembershipId,
    Guid OrganizationId,
    string Roles,
    string Status)
{
    public bool IsOwner => Roles.Split(',').Contains("Owner");
}
