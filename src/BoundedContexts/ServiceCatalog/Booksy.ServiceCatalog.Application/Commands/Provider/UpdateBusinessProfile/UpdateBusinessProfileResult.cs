// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/UpdateBusinessProfile/UpdateBusinessProfileResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessProfile
{
    public sealed record UpdateBusinessProfileResult(
        Guid ProviderId,
        string BusinessName,
        string Description,
        DateTime UpdatedAt);
}