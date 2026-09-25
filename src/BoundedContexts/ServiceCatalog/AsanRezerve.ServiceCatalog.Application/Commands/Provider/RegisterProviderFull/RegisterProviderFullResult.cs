// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Provider/RegisterProviderFull/RegisterProviderFullResult.cs
// ========================================

using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.RegisterProviderFull
{
    /// <summary>
    /// Result of full provider registration
    /// </summary>
    public sealed record RegisterProviderFullResult(
        Guid ProviderId,
        string BusinessName,
        ProviderStatus Status,
        DateTime RegisteredAt,
        int ServicesCount,
        int StaffCount);
}
