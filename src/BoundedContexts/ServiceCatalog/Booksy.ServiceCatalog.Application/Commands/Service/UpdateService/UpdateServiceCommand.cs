// ========================================
// Booksy.ServiceCatalog.Application/Commands/Service/UpdateService/UpdateServiceCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Service.UpdateService
{
    public sealed record UpdateServiceCommand(
        Guid ServiceId,
        string Name,
        string Description,
        string CategoryName,
        int DurationMinutes,
        int? PreparationMinutes = null,
        int? BufferMinutes = null,
        List<string>? Tags = null,
        string? ImageUrl = null,
        Guid? IdempotencyKey = null) : ICommand<UpdateServiceResult>;
}