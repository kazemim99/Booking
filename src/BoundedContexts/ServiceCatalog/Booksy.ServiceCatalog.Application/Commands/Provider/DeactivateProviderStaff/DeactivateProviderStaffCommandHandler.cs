// ========================================
// Application/Commands/Provider/DeactivateProviderStaff/DeactivateProviderStaffCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.DeactivateProviderStaff
{
    /// <summary>
    /// Handler for DeactivateProviderStaffCommand - deactivates staff through Provider aggregate
    /// ✅ DDD-Compliant: Uses IProviderWriteRepository only and calls provider.DeactivateStaff()
    /// </summary>
    internal sealed class DeactivateProviderStaffCommandHandler
        : ICommandHandler<DeactivateProviderStaffCommand, DeactivateProviderStaffResult>
    {
        private readonly IProviderWriteRepository _providerRepository;

        public DeactivateProviderStaffCommandHandler(IProviderWriteRepository providerRepository)
        {
            _providerRepository = providerRepository;
        }

        public async Task<DeactivateProviderStaffResult> Handle(
            DeactivateProviderStaffCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new ArgumentException("Deactivation reason is required", nameof(request.Reason));

            var providerId = ProviderId.From(request.ProviderId);
            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

            if (provider == null)
                throw new KeyNotFoundException($"Provider {request.ProviderId} not found");

            provider.DeactivateStaff(request.StaffId, request.Reason);

            await _providerRepository.UpdateProviderAsync(provider, cancellationToken);

            var deactivatedStaff = provider.GetStaffById(request.StaffId);
            if (deactivatedStaff == null)
                throw new InvalidOperationException($"Staff {request.StaffId} not found after deactivation");

            return new DeactivateProviderStaffResult(
                provider.Id.Value,
                deactivatedStaff.Id,
                deactivatedStaff.FullName,
                deactivatedStaff.IsActive,
                DateTime.UtcNow,
                request.Reason);
        }
    }
}
