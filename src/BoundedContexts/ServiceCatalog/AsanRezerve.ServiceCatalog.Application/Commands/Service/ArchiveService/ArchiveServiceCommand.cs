//===========================================
// Commands/Service/ArchiveService/ArchiveServiceCommand.cs
//===========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Service.ArchiveService
{
    public sealed record ArchiveServiceCommand(
        Guid ServiceId,
        string? Reason = null,
        Guid? IdempotencyKey = null) : ICommand<ArchiveServiceResult>;
}
