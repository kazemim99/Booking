using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.CancelJoinRequest
{
    public sealed class CancelJoinRequestCommandHandler : ICommandHandler<CancelJoinRequestCommand, CancelJoinRequestResult>
    {
        private readonly IProviderJoinRequestReadRepository _joinRequestReadRepository;
        private readonly IProviderJoinRequestWriteRepository _joinRequestWriteRepository;
        private readonly IProviderReadRepository _providerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CancelJoinRequestCommandHandler> _logger;

        public CancelJoinRequestCommandHandler(
            IProviderJoinRequestReadRepository joinRequestReadRepository,
            IProviderJoinRequestWriteRepository joinRequestWriteRepository,
            IProviderReadRepository providerRepository,
            IUnitOfWork unitOfWork,
            ILogger<CancelJoinRequestCommandHandler> logger)
        {
            _joinRequestReadRepository = joinRequestReadRepository;
            _joinRequestWriteRepository = joinRequestWriteRepository;
            _providerRepository = providerRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CancelJoinRequestResult> Handle(CancelJoinRequestCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Canceling join request {RequestId} by requester {RequesterId}",
                request.RequestId, request.RequesterId);

            // Get the join request
            var joinRequest = await _joinRequestReadRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (joinRequest == null)
                throw new NotFoundException($"Join request with ID {request.RequestId} not found");

            // Verify the requester is the one who created the request
            var requesterId = ProviderId.From(request.RequesterId);
            if (joinRequest.RequesterId != requesterId)
                throw new DomainValidationException("Only the requester can cancel their own join request");

            // Validate requester exists
            var requester = await _providerRepository.GetByIdAsync(requesterId, cancellationToken);
            if (requester == null)
                throw new NotFoundException($"Provider with ID {request.RequesterId} not found");

            // Withdraw the request (sets status to Withdrawn)
            joinRequest.Withdraw();

            // Save changes
            await _joinRequestWriteRepository.UpdateAsync(joinRequest, cancellationToken);
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation("Join request {RequestId} has been withdrawn", request.RequestId);

            return new CancelJoinRequestResult(
                RequestId: joinRequest.Id,
                Status: joinRequest.Status.ToString());
        }
    }
}
