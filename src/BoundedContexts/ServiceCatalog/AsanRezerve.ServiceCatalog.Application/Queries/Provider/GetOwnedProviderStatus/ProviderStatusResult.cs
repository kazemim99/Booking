// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Provider/GetOwnedProviderStatus/ProviderStatusResult.cs
// ========================================
using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetOwnedProviderStatus
{
    /// <summary>
    /// Result DTO for Provider status query
    /// </summary>
    public sealed record ProviderStatusResult(
        Guid ProviderId,
        ProviderStatus Status,
        Guid UserId);
}
