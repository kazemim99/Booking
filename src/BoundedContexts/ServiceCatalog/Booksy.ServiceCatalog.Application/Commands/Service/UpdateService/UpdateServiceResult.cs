// ========================================
// Booksy.ServiceCatalog.Application/Commands/Service/UpdateService/UpdateServiceResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Service.UpdateService
{
    public sealed record UpdateServiceResult(
        Guid ServiceId,
        string Name,
        string Description,
        DateTime UpdatedAt);
}