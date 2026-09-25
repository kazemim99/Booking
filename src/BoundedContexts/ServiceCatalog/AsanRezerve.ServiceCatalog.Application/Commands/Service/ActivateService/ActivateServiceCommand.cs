// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Service/ActivateService/ActivateServiceCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Service.ActivateService
{
    public sealed record ActivateServiceCommand(
        Guid ServiceId,
        Guid? IdempotencyKey = null) : ICommand<ActivateServiceResult>;
}