namespace Booksy.ServiceCatalog.Api.Models.Requests;

/// <summary>Optional reason recorded with an invitation revocation.</summary>
public sealed record RevokeInvitationRequest(string? Reason);
