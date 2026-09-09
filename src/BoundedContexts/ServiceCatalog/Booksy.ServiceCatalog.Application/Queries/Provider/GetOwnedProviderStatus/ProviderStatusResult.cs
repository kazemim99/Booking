// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetCurrentProviderStatus/ProviderStatusResult.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetCurrentProviderStatus
{
    /// <summary>
    /// Result DTO for Provider status query
    /// </summary>
    public sealed record ProviderStatusResult(
        Guid ProviderId,
        ProviderStatus Status,
        Guid UserId);
}
