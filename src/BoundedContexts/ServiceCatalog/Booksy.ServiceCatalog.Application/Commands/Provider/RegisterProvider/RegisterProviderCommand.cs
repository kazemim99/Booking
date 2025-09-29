// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/RegisterProvider/RegisterProviderCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProvider
{
    /// <summary>
    /// Command to register a new service provider
    /// </summary>
    public sealed record RegisterProviderCommand(
        Guid OwnerId,
        string BusinessName,
        string Description,
        BusinessSize ProviderType,
        string PrimaryPhone,
        string? SecondaryPhone,
        string? Website,
        string Street,
        string City,
        string State,
        string PostalCode,
        string Country,
        double? Latitude = null,
        double? Longitude = null,
        Guid? IdempotencyKey = null) : ICommand<RegisterProviderResult>
    {
        public string Email { get; internal set; }
    }
}