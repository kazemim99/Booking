// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/RegisterProvider/RegisterProviderResult.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProvider
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