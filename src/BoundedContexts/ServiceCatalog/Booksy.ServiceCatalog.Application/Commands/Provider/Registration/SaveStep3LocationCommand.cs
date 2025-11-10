using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.Registration;

/// <summary>
/// Step 3: Save location information and create provider draft
/// This is the first step that persists data to the database
/// </summary>
public sealed record SaveStep3LocationCommand(
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
    Guid? IdempotencyKey = null
) : ICommand<SaveStep3LocationResult>;

public sealed record SaveStep3LocationResult(
    Guid ProviderId,
    int RegistrationStep,
    string Message,
    string? AccessToken = null,
    string? RefreshToken = null,
    int? ExpiresIn = null
);
