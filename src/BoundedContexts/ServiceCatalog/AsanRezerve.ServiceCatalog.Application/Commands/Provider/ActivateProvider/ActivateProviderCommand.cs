// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Provider/ActivateProvider/ActivateProviderCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.ActivateProvider
{
    public sealed record ActivateProviderCommand(
        Guid ProviderId,
        Guid? IdempotencyKey = null) : ICommand<ActivateProviderResult>;
}