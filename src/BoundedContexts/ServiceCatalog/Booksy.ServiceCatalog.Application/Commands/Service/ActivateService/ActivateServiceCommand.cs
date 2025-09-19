// ========================================
// Booksy.ServiceCatalog.Application/Commands/Service/ActivateService/ActivateServiceCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Service.ActivateService
{
    public sealed record ActivateServiceCommand(
        Guid ServiceId,
        Guid? IdempotencyKey = null) : ICommand<ActivateServiceResult>;
}