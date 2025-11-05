// ========================================
// Application/Commands/Provider/ActivateProviderStaff/ActivateProviderStaffCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.ActivateProviderStaff
{
 
    internal sealed class ActivateProviderStaffCommandHandler
        : ICommandHandler<ActivateProviderStaffCommand, ActivateProviderStaffResult>
    {
        private readonly IProviderWriteRepository _providerRepository;

        public ActivateProviderStaffCommandHandler(IProviderWriteRepository providerRepository)
        {
            _providerRepository = providerRepository;
        }

        public async Task<ActivateProviderStaffResult> Handle(
            ActivateProviderStaffCommand request,
            CancellationToken cancellationToken)
        {
            // ✅ Load Provider aggregate (not Staff directly)
            var providerId = ProviderId.From(request.ProviderId);
            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

            if (provider == null)
                throw new KeyNotFoundException($"Provider {request.ProviderId} not found");

            // ✅ Activate staff through Provider aggregate root
            provider.ActivateStaff(request.StaffId);

            // ✅ Save Provider aggregate (EF Core cascades to staff)
            await _providerRepository.UpdateProviderAsync(provider, cancellationToken);

            // Get activated staff for response
            var activatedStaff = provider.GetStaffById(request.StaffId);
            if (activatedStaff == null)
                throw new InvalidOperationException($"Staff {request.StaffId} not found after activation");

            return new ActivateProviderStaffResult(
                provider.Id.Value,
                activatedStaff.Id,
                activatedStaff.FullName,
                activatedStaff.IsActive,
                DateTime.UtcNow);
        }
    }
}
