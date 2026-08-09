using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Membership.GetInvitationSummary;

/// <summary>
/// Public, anonymous summary of an invitation for the accept screen opened from an
/// SMS link (the opener has no account/JWT yet). The phone is masked; only enough is
/// returned to show who invited them and whether the invitation is still valid.
/// </summary>
public sealed record GetInvitationSummaryQuery(Guid InvitationId)
    : IQuery<InvitationSummaryResult?>;

public sealed record InvitationSummaryResult(
    Guid InvitationId,
    Guid OrganizationId,
    string OrganizationName,
    string? OrganizationLogo,
    string? InviteeName,
    string MaskedPhone,
    string Status,
    DateTime ExpiresAt,
    bool IsValid);
