using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.RejectJoinRequest
{
    public sealed class RejectJoinRequestCommandHandler : ICommandHandler<RejectJoinRequestCommand, RejectJoinRequestResult>
    {
        private readonly IProviderJoinRequestReadRepository _joinRequestReadRepository;
        private readonly IProviderJoinRequestWriteRepository _joinRequestWriteRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RejectJoinRequestCommandHandler> _logger;

        public RejectJoinRequestCommandHandler(
            IProviderJoinRequestReadRepository joinRequestReadRepository,
            IProviderJoinRequestWriteRepository joinRequestWriteRepository,
            IUnitOfWork unitOfWork,
            ILogger<RejectJoinRequestCommandHandler> logger)
        {
            _joinRequestReadRepository = joinRequestReadRepository;
            _joinRequestWriteRepository = joinRequestWriteRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<RejectJoinRequestResult> Handle(RejectJoinRequestCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Rejecting join request {RequestId}", request.RequestId);

            // Get join request
            var joinRequest = await _joinRequestReadRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (joinRequest == null)
                throw new NotFoundException($"Join request with ID {request.RequestId} not found");

            if (joinRequest.Status != JoinRequestStatus.Pending)
                throw new DomainValidationException($"Join request is no longer pending (status: {joinRequest.Status})");

            // Reject request
            joinRequest.Reject(request.ReviewerId, request.Reason);

            await _joinRequestWriteRepository.UpdateAsync(joinRequest, cancellationToken);

            _logger.LogInformation("Join request {RequestId} rejected by reviewer {ReviewerId}",
                joinRequest.Id, request.ReviewerId);

            return new RejectJoinRequestResult(
                RequestId: joinRequest.Id,
                OrganizationId: joinRequest.OrganizationId.Value,
                RequesterId: joinRequest.RequesterId.Value,
                RejectedAt: joinRequest.ReviewedAt!.Value);
        }
    }
}
