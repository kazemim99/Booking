// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetOwnedProviderStatus/ProviderStatusResult.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetOwnedProviderStatus
{
    /// <summary>
    /// Result DTO for Provider status query
    /// </summary>
    public sealed record ProviderStatusResult(
        Guid ProviderId,
        ProviderStatus Status,
        Guid UserId);
}
