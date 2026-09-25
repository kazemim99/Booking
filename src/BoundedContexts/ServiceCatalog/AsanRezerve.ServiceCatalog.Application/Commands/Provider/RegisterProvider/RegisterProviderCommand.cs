// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Provider/RegisterProvider/RegisterProviderCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.RegisterProvider
{
    /// <summary>
    /// Command to register a new service provider
    /// </summary>
    public sealed record RegisterProviderCommand(
        Guid OwnerId,
        string OwnerFirstName,
        string OwnerLastName,
        string BusinessName,
        string Description,
        string Email,
        ServiceCategory PrimaryCategory,
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
    }
}