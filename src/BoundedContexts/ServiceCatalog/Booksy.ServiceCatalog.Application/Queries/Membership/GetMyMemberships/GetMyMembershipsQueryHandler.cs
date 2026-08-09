using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Booksy.ServiceCatalog.Application.Queries.Membership.GetMyMemberships;

public sealed class GetMyMembershipsQueryHandler : IQueryHandler<GetMyMembershipsQuery, GetMyMembershipsResult>
{
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IProviderReadRepository _providerRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetMyMembershipsQueryHandler> _logger;

    public GetMyMembershipsQueryHandler(
        IOrganizationMembershipRepository membershipRepository,
        IProviderReadRepository providerRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GetMyMembershipsQueryHandler> logger)
    {
        _membershipRepository = membershipRepository;
        _providerRepository = providerRepository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<GetMyMembershipsResult> Handle(GetMyMembershipsQuery request, CancellationToken cancellationToken)
    {
        var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr))
            throw new UnauthorizedAccessException("User not authenticated");

        var personId = UserId.From(userIdStr);

        var memberships = await _membershipRepository.GetByPersonAsync(personId, cancellationToken);

        var dtos = new List<MyMembershipDto>();
        foreach (var membership in memberships)
        {
            if (membership.Status == MembershipStatus.Terminated)
                continue;

            var organization = await _providerRepository.GetByIdAsync(membership.OrganizationId, cancellationToken);

            dtos.Add(new MyMembershipDto(
                MembershipId: membership.Id,
                OrganizationId: membership.OrganizationId.Value,
                OrganizationName: organization?.Profile.BusinessName ?? string.Empty,
                OrganizationLogo: organization?.Profile.LogoUrl,
                Roles: membership.Roles.Select(r => r.ToString()).ToList(),
                Status: membership.Status.ToString(),
                ProvidesServices: membership.ProvidesServices,
                JoinedAt: membership.JoinedAt));
        }

        _logger.LogDebug("Person {PersonId} has {Count} active membership(s)", personId.Value, dtos.Count);

        return new GetMyMembershipsResult(dtos);
    }
}
