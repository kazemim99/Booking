// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/ActivateProvider/ActivateProviderResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Provider.ActivateProvider
{
    public sealed record ActivateProviderResult(
        Guid ProviderId,
        string BusinessName,
        DateTime ActivatedAt);
}