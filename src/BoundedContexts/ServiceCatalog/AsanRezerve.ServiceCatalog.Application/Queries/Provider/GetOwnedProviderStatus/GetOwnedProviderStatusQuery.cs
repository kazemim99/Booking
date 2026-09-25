// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Provider/GetOwnedProviderStatus/GetOwnedProviderStatusQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetOwnedProviderStatus
{
    /// <summary>
    /// The status of the salon the current authenticated user OWNS. Null when they own none —
    /// including for an employee, who is a provider through a membership but owns nothing; use
    /// <c>GetMyMemberships</c> for "which salons am I part of?". No parameters: the caller comes
    /// from the HttpContext. (Renamed from <c>GetCurrentProviderStatusQuery</c>, which read as
    /// "am I a provider?" and was mistaken for it.)
    /// </summary>
    public sealed record GetOwnedProviderStatusQuery() : IQuery<ProviderStatusResult?>;
}
