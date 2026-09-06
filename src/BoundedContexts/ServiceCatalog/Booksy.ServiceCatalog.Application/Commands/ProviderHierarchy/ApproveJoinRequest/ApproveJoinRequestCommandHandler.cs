using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
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
        private readonly IOrganizationMembershipRepository _membershipRepository;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<ApproveJoinRequestCommandHandler> _logger;

        public ApproveJoinRequestCommandHandler(
            IProviderReadRepository providerReadRepository,
            IProviderWriteRepository providerWriteRepository,
            IProviderJoinRequestReadRepository joinRequestReadRepository,
            IProviderJoinRequestWriteRepository joinRequestWriteRepository,
            IOrganizationMembershipRepository membershipRepository,
            IServiceCatalogUnitOfWork unitOfWork,
            ILogger<ApproveJoinRequestCommandHandler> logger)
        {
            _providerReadRepository = providerReadRepository;
            _providerWriteRepository = providerWriteRepository;
            _joinRequestReadRepository = joinRequestReadRepository;
            _joinRequestWriteRepository = joinRequestWriteRepository;
            _membershipRepository = membershipRepository;
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

            // refactor-identity-and-membership §4.3: the requester's owner also becomes a
            // real membership of the parent organization (Manager — they administer their
            // own linked business but are not the org's Owner, which stays with whoever
            // founded it), so they show up in the org's roster/staff list and any future
            // membership-scoped authorization sees them. Additive only: this does NOT
            // retire ParentProviderId/LinkToOrganization (§2.7, still open) — the sub-
            // provider hierarchy keeps working exactly as before; the requester's business
            // now also has a membership recorded alongside it. Guarded against ever
            // double-creating one, since a join request could in principle be re-approved
            // after some other path already granted a membership.
            if (!await _membershipRepository.HasActiveMembershipAsync(
                    requester.OwnerId, joinRequest.OrganizationId, cancellationToken))
            {
                var membership = OrganizationMembership.InviteExisting(
                    requester.OwnerId, joinRequest.OrganizationId, new[] { MembershipRole.Manager });
                membership.Accept();
                await _membershipRepository.SaveAsync(membership, cancellationToken);
            }

            // Persist (save first, then dispatch domain events).
            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

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
