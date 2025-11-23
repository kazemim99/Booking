using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.CreateJoinRequest
{
    public sealed class CreateJoinRequestCommandHandler : ICommandHandler<CreateJoinRequestCommand, CreateJoinRequestResult>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IProviderJoinRequestReadRepository _joinRequestReadRepository;
        private readonly IProviderJoinRequestWriteRepository _joinRequestWriteRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateJoinRequestCommandHandler> _logger;

        public CreateJoinRequestCommandHandler(
            IProviderReadRepository providerRepository,
            IProviderJoinRequestReadRepository joinRequestReadRepository,
            IProviderJoinRequestWriteRepository joinRequestWriteRepository,
            IUnitOfWork unitOfWork,
            ILogger<CreateJoinRequestCommandHandler> logger)
        {
            _providerRepository = providerRepository;
            _joinRequestReadRepository = joinRequestReadRepository;
            _joinRequestWriteRepository = joinRequestWriteRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CreateJoinRequestResult> Handle(CreateJoinRequestCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating join request from {RequesterId} to organization {OrganizationId}",
                request.RequesterId, request.OrganizationId);

            var organizationId = ProviderId.From(request.OrganizationId);
            var requesterId = ProviderId.From(request.RequesterId);

            // Validate organization
            var organization = await _providerRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
                throw new NotFoundException($"Organization with ID {request.OrganizationId} not found");

            if (organization.HierarchyType != ProviderHierarchyType.Organization)
                throw new DomainValidationException("Can only join organizations");

            // Validate requester
            var requester = await _providerRepository.GetByIdAsync(requesterId, cancellationToken);
            if (requester == null)
                throw new NotFoundException($"Provider with ID {request.RequesterId} not found");

            if (requester.HierarchyType != ProviderHierarchyType.Individual)
                throw new DomainValidationException("Only individual providers can request to join organizations");

            if (requester.ParentProviderId != null)
                throw new DomainValidationException("Provider is already linked to an organization");

            // Check for existing pending request
            var hasPendingRequest = await _joinRequestReadRepository.HasPendingRequestAsync(
                requesterId, organizationId, cancellationToken);

            if (hasPendingRequest)
                throw new DomainValidationException("A pending join request already exists for this organization");

            // Create join request
            var joinRequest = ProviderJoinRequest.Create(organizationId, requesterId, request.Message);

            await _joinRequestWriteRepository.SaveAsync(joinRequest, cancellationToken);
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation("Join request {RequestId} created successfully", joinRequest.Id);

            return new CreateJoinRequestResult(
                RequestId: joinRequest.Id,
                OrganizationId: organizationId.Value,
                RequesterId: requesterId.Value,
                Status: joinRequest.Status.ToString(),
                CreatedAt: joinRequest.CreatedAt);
        }
    }
}
