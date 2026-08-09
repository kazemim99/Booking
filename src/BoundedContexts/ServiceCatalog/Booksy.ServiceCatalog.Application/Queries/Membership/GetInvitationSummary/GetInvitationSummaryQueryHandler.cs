using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Membership.GetInvitationSummary;

public sealed class GetInvitationSummaryQueryHandler
    : IQueryHandler<GetInvitationSummaryQuery, InvitationSummaryResult?>
{
    private readonly IProviderInvitationReadRepository _invitationRepository;
    private readonly IProviderReadRepository _providerRepository;
    private readonly ILogger<GetInvitationSummaryQueryHandler> _logger;

    public GetInvitationSummaryQueryHandler(
        IProviderInvitationReadRepository invitationRepository,
        IProviderReadRepository providerRepository,
        ILogger<GetInvitationSummaryQueryHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _providerRepository = providerRepository;
        _logger = logger;
    }

    public async Task<InvitationSummaryResult?> Handle(
        GetInvitationSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken);
        if (invitation is null)
            return null;

        var organization = await _providerRepository.GetByIdAsync(invitation.OrganizationId, cancellationToken);

        return new InvitationSummaryResult(
            InvitationId: invitation.Id,
            OrganizationId: invitation.OrganizationId.Value,
            OrganizationName: organization?.Profile.BusinessName ?? string.Empty,
            OrganizationLogo: organization?.Profile.LogoUrl,
            InviteeName: invitation.InviteeName,
            MaskedPhone: Mask(invitation.PhoneNumber.Value),
            Status: invitation.Status.ToString(),
            ExpiresAt: invitation.ExpiresAt,
            IsValid: invitation.IsValid());
    }

    // Show only the last 4 digits; everything else is a bullet.
    private static string Mask(string phone)
    {
        if (string.IsNullOrEmpty(phone))
            return string.Empty;
        if (phone.Length <= 4)
            return new string('•', phone.Length);
        return new string('•', phone.Length - 4) + phone[^4..];
    }
}
