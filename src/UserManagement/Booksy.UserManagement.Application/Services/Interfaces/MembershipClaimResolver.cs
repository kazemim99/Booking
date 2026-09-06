namespace Booksy.UserManagement.Application.Services.Interfaces;

/// <summary>
/// Shared "which membership is active" rule for every token-issuing handler (refactor-
/// identity-and-membership §5.4), so the three call sites (OTP sign-in, password sign-in,
/// refresh) cannot drift on what "active" means.
/// </summary>
public static class MembershipClaimResolver
{
    /// <summary>
    /// The membership for the organization this person owns/manages (matching the
    /// resolved providerId) takes priority; otherwise the first membership, if any.
    /// </summary>
    public static string? ResolveActiveMembershipId(
        IReadOnlyList<MembershipSummary>? memberships, string? providerId)
    {
        if (memberships is null || memberships.Count == 0)
            return null;

        if (providerId != null && Guid.TryParse(providerId, out var ownOrgId))
        {
            var own = memberships.FirstOrDefault(m => m.OrganizationId == ownOrgId);
            if (own != null)
                return own.MembershipId.ToString();
        }

        return memberships[0].MembershipId.ToString();
    }
}
