//===========================================
// Commands/Service/ArchiveService/ArchiveServiceCommand.cs
//===========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Service.ArchiveService
{
    public sealed record ArchiveServiceCommand(
        Guid ServiceId,
        string? Reason = null,
        Guid? IdempotencyKey = null) : ICommand<ArchiveServiceResult>;
}
