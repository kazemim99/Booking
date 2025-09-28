
namespace Booksy.ServiceCatalog.Application.Commands.Service.ArchiveService
{
    public sealed record ArchiveServiceResult(
        Guid ServiceId,
        string Name,
        Guid ProviderId,
        string? Reason);
}


