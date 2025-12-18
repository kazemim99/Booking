using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.ApproveJoinRequest
{
    public sealed class ApproveJoinRequestCommandHandler : ICommandHandler<ApproveJoinRequestCommand, ApproveJoinRequestResult>
    {
        private readonly IProviderReadRepository _providerReadRepository;
        private readonly IProviderWriteRepository _providerWriteRepository;
        private readonly IProviderJoinRequestReadRepository _joinRequestReadRepository;
        private readonly IProviderJoinRequestWriteRepository _joinRequestWriteRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ApproveJoinRequestCommandHandler> _logger;

        public ApproveJoinRequestCommandHandler(
            IProviderReadRepository providerReadRepository,
            IProviderWriteRepository providerWriteRepository,
            IProviderJoinRequestReadRepository joinRequestReadRepository,
            IProviderJoinRequestWriteRepository joinRequestWriteRepository,
            IUnitOfWork unitOfWork,
            ILogger<ApproveJoinRequestCommandHandler> logger)
        {
            _providerReadRepository = providerReadRepository;
            _providerWriteRepository = providerWriteRepository;
            _joinRequestReadRepository = joinRequestReadRepository;
            _joinRequestWriteRepository = joinRequestWriteRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApproveJoinRequestResult> Handle(ApproveJoinRequestCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Approving join request {RequestId}", request.RequestId);

            // Get join request
            var joinRequest = await _joinRequestReadRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (joinRequest == null)
                throw new NotFoundException($"Join request with ID {request.RequestId} not found");

            if (joinRequest.Status != JoinRequestStatus.Pending)
                throw new DomainValidationException($"Join request is no longer pending (status: {joinRequest.Status})");

            // Get requester provider
            var requester = await _providerReadRepository.GetByIdAsync(joinRequest.RequesterId, cancellationToken);
            if (requester == null)
                throw new NotFoundException($"Requester provider with ID {joinRequest.RequesterId} not found");

            if (requester.ParentProviderId != null)
                throw new DomainValidationException("Requester is already linked to an organization");

            // Approve request and link provider
            joinRequest.Approve(request.ReviewerId, request.Note);
            requester.LinkToOrganization(joinRequest.OrganizationId);

            await _joinRequestWriteRepository.UpdateAsync(joinRequest, cancellationToken);
            await _providerWriteRepository.UpdateAsync(requester, cancellationToken);

            _logger.LogInformation("Join request {RequestId} approved. Provider {RequesterId} linked to organization {OrganizationId}",
                joinRequest.Id, joinRequest.RequesterId, joinRequest.OrganizationId);

            return new ApproveJoinRequestResult(
                RequestId: joinRequest.Id,
                OrganizationId: joinRequest.OrganizationId.Value,
                RequesterId: joinRequest.RequesterId.Value,
                ApprovedAt: joinRequest.ReviewedAt!.Value);
        }
    }
}
