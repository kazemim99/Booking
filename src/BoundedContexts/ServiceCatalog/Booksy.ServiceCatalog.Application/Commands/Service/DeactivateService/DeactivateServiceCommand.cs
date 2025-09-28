using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Service.DeactivateService
{
    public sealed record DeactivateServiceCommand(
        Guid ServiceId,
        string? Reason = null,
        Guid? IdempotencyKey = null) : ICommand<DeactivateServiceResult>;
}
