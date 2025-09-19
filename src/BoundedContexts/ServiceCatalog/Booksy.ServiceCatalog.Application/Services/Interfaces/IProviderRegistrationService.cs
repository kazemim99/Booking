// ========================================
// Booksy.ServiceCatalog.Application/Services/Interfaces/IProviderRegistrationService.cs
// ========================================
using Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProvider;

namespace Booksy.ServiceCatalog.Application.Services.Interfaces
{
    public interface IProviderRegistrationService
    {
        Task ValidateRegistrationAsync(RegisterProviderCommand command, CancellationToken cancellationToken = default);
        Task<bool> IsBusinessNameAvailableAsync(string businessName, CancellationToken cancellationToken = default);
        Task<bool> IsOwnerEligibleAsync(Guid ownerId, CancellationToken cancellationToken = default);
        Task<bool> ValidateBusinessAddressAsync(string street, string city, string state, string postalCode, string country, CancellationToken cancellationToken = default);
    }
}