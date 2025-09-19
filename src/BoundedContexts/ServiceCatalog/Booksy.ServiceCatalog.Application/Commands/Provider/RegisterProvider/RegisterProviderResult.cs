// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/RegisterProvider/RegisterProviderResult.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProvider
{
    public sealed record RegisterProviderResult(
        Guid ProviderId,
        string BusinessName,
        ProviderStatus Status,
        DateTime RegisteredAt);
}