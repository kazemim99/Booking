using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetPendingJoinRequests
{
    public sealed class GetPendingJoinRequestsQueryHandler : IQueryHandler<GetPendingJoinRequestsQuery, GetPendingJoinRequestsResult>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IProviderJoinRequestReadRepository _joinRequestRepository;
        private readonly ILogger<GetPendingJoinRequestsQueryHandler> _logger;

        public GetPendingJoinRequestsQueryHandler(
            IProviderReadRepository providerRepository,
            IProviderJoinRequestReadRepository joinRequestRepository,
            ILogger<GetPendingJoinRequestsQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _joinRequestRepository = joinRequestRepository;
            _logger = logger;
        }

        public async Task<GetPendingJoinRequestsResult> Handle(GetPendingJoinRequestsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Getting pending join requests for organization {OrganizationId}", request.OrganizationId);

            var organizationId = ProviderId.From(request.OrganizationId);

            // Validate organization
            var organization = await _providerRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
                throw new NotFoundException($"Organization with ID {request.OrganizationId} not found");

            if (organization.HierarchyType != ProviderHierarchyType.Organization)
                throw new DomainValidationException("Provider is not an organization");

            // Get pending join requests
            var joinRequests = await _joinRequestRepository.GetByOrganizationIdAndStatusAsync(
                organizationId, JoinRequestStatus.Pending, cancellationToken);

            // Enrich with requester details
            var joinRequestDtos = new List<JoinRequestDto>();
            foreach (var jr in joinRequests)
            {
                var requester = await _providerRepository.GetByIdAsync(jr.RequesterId, cancellationToken);
                joinRequestDtos.Add(new JoinRequestDto(
                    RequestId: jr.Id,
                    RequesterId: jr.RequesterId.Value,
                    RequesterName: requester?.Profile.BusinessName ?? "Unknown",
                    RequesterProfileImageUrl: requester?.Profile.ProfileImageUrl,
                    Message: jr.Message,
                    Status: jr.Status.ToString(),
                    CreatedAt: jr.CreatedAt));
            }

            return new GetPendingJoinRequestsResult(
                OrganizationId: organizationId.Value,
                JoinRequests: joinRequestDtos);
        }
    }
}
