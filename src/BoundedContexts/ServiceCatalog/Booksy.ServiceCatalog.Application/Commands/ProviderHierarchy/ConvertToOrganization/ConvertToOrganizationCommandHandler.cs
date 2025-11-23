using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.ConvertToOrganization
{
    public sealed class ConvertToOrganizationCommandHandler : ICommandHandler<ConvertToOrganizationCommand, ConvertToOrganizationResult>
    {
        private readonly IProviderReadRepository _providerReadRepository;
        private readonly IProviderWriteRepository _providerWriteRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ConvertToOrganizationCommandHandler> _logger;

        public ConvertToOrganizationCommandHandler(
            IProviderReadRepository providerReadRepository,
            IProviderWriteRepository providerWriteRepository,
            IUnitOfWork unitOfWork,
            ILogger<ConvertToOrganizationCommandHandler> logger)
        {
            _providerReadRepository = providerReadRepository;
            _providerWriteRepository = providerWriteRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ConvertToOrganizationResult> Handle(ConvertToOrganizationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Converting provider {ProviderId} to organization", request.ProviderId);

            var providerId = ProviderId.From(request.ProviderId);
            var provider = await _providerReadRepository.GetByIdAsync(providerId, cancellationToken);

            if (provider == null)
                throw new NotFoundException($"Provider with ID {request.ProviderId} not found");

            if (provider.HierarchyType == ProviderHierarchyType.Organization)
                throw new DomainValidationException("Provider is already an organization");

            if (provider.ParentProviderId != null)
                throw new DomainValidationException("Cannot convert a staff member to organization. Must leave parent organization first.");

            // Convert to organization
            provider.ConvertToOrganization();

            await _providerWriteRepository.UpdateAsync(provider, cancellationToken);
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation("Provider {ProviderId} converted to organization", provider.Id);

            return new ConvertToOrganizationResult(
                ProviderId: provider.Id.Value,
                HierarchyType: provider.HierarchyType.ToString(),
                ConvertedAt: DateTime.UtcNow);
        }
    }
}
