// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Provider/UpdateBusinessProfile/UpdateBusinessProfileCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.UpdateBusinessProfile
{
    public sealed record UpdateBusinessProfileCommand(
        Guid ProviderId,
        string BusinessName,
        string Description,
        string? Website = null,
        string? LogoUrl = null,
        Guid? IdempotencyKey = null) : ICommand<UpdateBusinessProfileResult>;
}