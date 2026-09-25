//===========================================
// Commands/Service/DeactivateService/DeactivateServiceResult.cs
//===========================================
namespace AsanRezerve.ServiceCatalog.Application.Commands.Service.DeactivateService
{
    public sealed record DeactivateServiceResult(
        Guid ServiceId,
        string Name,
        Guid ProviderId,
        string? Reason);
}


