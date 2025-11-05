using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.Registration;

/// <summary>
/// Step 4: Save services to provider draft
/// Requires Step 3 to be completed (provider draft must exist)
/// </summary>
public sealed record SaveStep4ServicesCommand(
    Guid ProviderId,
    List<ServiceDto> Services,
    Guid? IdempotencyKey = null
) : ICommand<SaveStep4ServicesResult>;

public sealed record ServiceDto(
    string Name,
    int DurationHours,
    int DurationMinutes,
    decimal Price,
    string PriceType // "fixed" or "variable"
);

public sealed record SaveStep4ServicesResult(
    Guid ProviderId,
    int RegistrationStep,
    int ServicesCount,
    string Message
);
