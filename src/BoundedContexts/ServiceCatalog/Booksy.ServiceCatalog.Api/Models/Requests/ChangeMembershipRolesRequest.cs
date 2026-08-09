namespace Booksy.ServiceCatalog.Api.Models.Requests;

/// <summary>The full new set of organization-scoped roles for a membership.</summary>
public sealed record ChangeMembershipRolesRequest(IReadOnlyList<string> Roles);
