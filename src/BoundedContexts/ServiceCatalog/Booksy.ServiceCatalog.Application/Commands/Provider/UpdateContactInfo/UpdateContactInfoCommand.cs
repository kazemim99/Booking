// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/UpdateContactInfo/UpdateContactInfoCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateContactInfo
{
    public sealed record UpdateContactInfoCommand(
        Guid ProviderId,
        string Email,
        string PrimaryPhone,
        string? SecondaryPhone = null,
        string? Website = null,
        string? FacebookPage = null,
        string? InstagramHandle = null,
        Guid? IdempotencyKey = null) : ICommand<UpdateContactInfoResult>;
}