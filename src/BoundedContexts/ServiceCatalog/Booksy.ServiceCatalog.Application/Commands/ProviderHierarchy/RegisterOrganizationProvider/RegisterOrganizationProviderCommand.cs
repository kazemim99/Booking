using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.RegisterOrganizationProvider
{
    /// <summary>
    /// Command to register a new organization provider (business with potential staff)
    /// </summary>
    public sealed record RegisterOrganizationProviderCommand(
        string BusinessName,
        string BusinessDescription,
        string Category,
        string PhoneNumber,
        string Email,
        string AddressLine1,
        string? AddressLine2,
        string City,
        string Province,
        string PostalCode,
        decimal Latitude,
        decimal Longitude,
        string OwnerFirstName,
        string OwnerLastName,
        Guid? IdempotencyKey = null
    ) : ICommand<RegisterOrganizationProviderResult>;

    public sealed record RegisterOrganizationProviderResult(
        Guid ProviderId,
        string HierarchyType,
        int RegistrationStep,
        string Message);
}
