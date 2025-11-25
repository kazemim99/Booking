using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetSentJoinRequests
{
    public sealed class GetSentJoinRequestsQueryHandler : IQueryHandler<GetSentJoinRequestsQuery, GetSentJoinRequestsResult>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IProviderJoinRequestReadRepository _joinRequestRepository;
        private readonly ILogger<GetSentJoinRequestsQueryHandler> _logger;

        public GetSentJoinRequestsQueryHandler(
            IProviderReadRepository providerRepository,
            IProviderJoinRequestReadRepository joinRequestRepository,
            ILogger<GetSentJoinRequestsQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _joinRequestRepository = joinRequestRepository;
            _logger = logger;
        }

        public async Task<GetSentJoinRequestsResult> Handle(GetSentJoinRequestsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Getting sent join requests for individual provider {ProviderId}", request.IndividualProviderId);

            var individualId = ProviderId.From(request.IndividualProviderId);

            // Validate individual provider exists
            var individual = await _providerRepository.GetByIdAsync(individualId, cancellationToken);
            if (individual == null)
                throw new NotFoundException($"Provider with ID {request.IndividualProviderId} not found");

            if (individual.HierarchyType != ProviderHierarchyType.Individual)
                throw new DomainValidationException("Provider is not an individual");

            // Get all join requests sent by this individual (all statuses)
            var joinRequests = await _joinRequestRepository.GetByRequesterIdAsync(individualId, cancellationToken);

            // Enrich with organization details
            var joinRequestDtos = new List<SentJoinRequestDto>();
            foreach (var jr in joinRequests)
            {
                var organization = await _providerRepository.GetByIdAsync(jr.OrganizationId, cancellationToken);
                joinRequestDtos.Add(new SentJoinRequestDto(
                    RequestId: jr.Id,
                    OrganizationId: jr.OrganizationId.Value,
                    OrganizationName: organization?.Profile.BusinessName ?? "Unknown Organization",
                    OrganizationLogoUrl: organization?.Profile.LogoUrl,
                    Message: jr.Message,
                    Status: jr.Status.ToString(),
                    CreatedAt: jr.CreatedAt,
                    RespondedAt: jr.ReviewedAt,
                    RejectionReason: jr.ReviewNote));
            }

            return new GetSentJoinRequestsResult(
                IndividualProviderId: individualId.Value,
                JoinRequests: joinRequestDtos);
        }
    }
}
