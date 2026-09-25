// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Provider/UpdateBusinessProfile/UpdateBusinessProfileResult.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.UpdateBusinessProfile
{
    public sealed record UpdateBusinessProfileResult(
        Guid ProviderId,
        string BusinessName,
        string Description,
        DateTime UpdatedAt);
}