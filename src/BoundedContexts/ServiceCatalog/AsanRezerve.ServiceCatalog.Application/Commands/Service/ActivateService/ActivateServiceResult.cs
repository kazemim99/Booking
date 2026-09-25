// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Service/ActivateService/ActivateServiceResult.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Commands.Service.ActivateService
{
    public sealed record ActivateServiceResult(
        Guid ServiceId,
        string Name,
        Guid ProviderId,
        DateTime ActivatedAt);
}