namespace Booksy.ServiceCatalog.Api.Models.Requests;

/// <summary>Optional reason for terminating a membership.</summary>
public sealed record TerminateMembershipRequest(string? Reason);
