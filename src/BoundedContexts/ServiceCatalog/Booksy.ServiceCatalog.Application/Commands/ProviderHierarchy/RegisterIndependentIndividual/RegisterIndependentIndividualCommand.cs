using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.RegisterIndependentIndividual
{
    /// <summary>
    /// Command to register a new independent individual provider (solo professional)
    /// </summary>
    public sealed record RegisterIndependentIndividualCommand(
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
        string FirstName,
        string LastName,
        Guid? IdempotencyKey = null
    ) : ICommand<RegisterIndependentIndividualResult>;

    public sealed record RegisterIndependentIndividualResult(
        Guid ProviderId,
        string HierarchyType,
        bool IsIndependent,
        int RegistrationStep,
        string Message);
}
