using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Service.DeactivateService
{
    public sealed record DeactivateServiceCommand(
        Guid ServiceId,
        string? Reason = null,
        Guid? IdempotencyKey = null) : ICommand<DeactivateServiceResult>;
}
