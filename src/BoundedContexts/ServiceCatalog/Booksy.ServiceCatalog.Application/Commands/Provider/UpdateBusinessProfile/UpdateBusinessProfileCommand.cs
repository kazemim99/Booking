// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/UpdateBusinessProfile/UpdateBusinessProfileCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessProfile
{
    public sealed record UpdateBusinessProfileCommand(
        Guid ProviderId,
        string BusinessName,
        string Description,
        string? Website = null,
        string? LogoUrl = null,
        List<string>? Tags = null,
        Dictionary<string, string>? SocialMedia = null,
        Guid? IdempotencyKey = null) : ICommand<UpdateBusinessProfileResult>;
}