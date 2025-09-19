// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/ActivateProvider/ActivateProviderCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.ActivateProvider
{
    public sealed record ActivateProviderCommand(
        Guid ProviderId,
        Guid? IdempotencyKey = null) : ICommand<ActivateProviderResult>;
}