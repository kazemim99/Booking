// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Provider/ActivateProvider/ActivateProviderResult.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.ActivateProvider
{
    public sealed record ActivateProviderResult(
        Guid ProviderId,
        string BusinessName,
        DateTime ActivatedAt);
}