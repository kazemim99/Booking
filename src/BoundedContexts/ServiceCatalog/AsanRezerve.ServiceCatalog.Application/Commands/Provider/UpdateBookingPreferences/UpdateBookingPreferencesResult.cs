using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.UpdateBookingPreferences;

/// <summary>
/// The provider's booking policy as stored. Echoing it back lets the settings UI confirm exactly what took effect
/// (rather than assuming its own optimistic state) and makes the change visible in API logs.
/// </summary>
public sealed record UpdateBookingPreferencesResult(
    Guid ProviderId,
    bool RequireDeposit,
    DepositType DepositType,
    decimal DepositPercentage,
    decimal DepositFixedAmount,
    int MinAdvanceBookingHours,
    int MaxAdvanceBookingDays,
    int CancellationWindowHours,
    decimal CancellationFeePercentage,
    bool AllowRescheduling,
    int RescheduleWindowHours,
    DateTime UpdatedAt);
