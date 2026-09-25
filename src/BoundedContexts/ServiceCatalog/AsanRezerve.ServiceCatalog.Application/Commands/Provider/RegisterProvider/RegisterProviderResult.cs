// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Provider/RegisterProvider/RegisterProviderResult.cs
// ========================================
using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.RegisterProvider
{
    public sealed record RegisterProviderResult(
        Guid ProviderId,
        string BusinessName,
        ServiceCategory PrimaryCategory,
        ProviderStatus Status,
        DateTime RegisteredAt,
        string AccessToken,
        string? RefreshToken,
        int ExpiresIn);
}