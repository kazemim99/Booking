// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetCurrentProviderStatus/GetCurrentProviderStatusQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetCurrentProviderStatus
{
    /// <summary>
    /// Query to get the current authenticated user's Provider status
    /// No parameters needed - uses authenticated user context from HttpContext
    /// </summary>
    public sealed record GetCurrentProviderStatusQuery() : IQuery<ProviderStatusResult?>;
}
