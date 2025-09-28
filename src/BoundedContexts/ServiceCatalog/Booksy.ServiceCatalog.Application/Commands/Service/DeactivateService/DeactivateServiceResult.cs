//===========================================
// Commands/Service/DeactivateService/DeactivateServiceResult.cs
//===========================================
namespace Booksy.ServiceCatalog.Application.Commands.Service.DeactivateService
{
    public sealed record DeactivateServiceResult(
        Guid ServiceId,
        string Name,
        Guid ProviderId,
        string? Reason);
}


