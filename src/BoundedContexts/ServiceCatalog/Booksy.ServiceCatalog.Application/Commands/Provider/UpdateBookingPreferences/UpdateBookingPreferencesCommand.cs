using Booksy.Core.Application.Abstractions;
using Booksy.Core.Application.Authorization;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBookingPreferences;

/// <summary>
/// Sets a provider's default booking policy — including whether a deposit is required (P0-0).
///
/// <para>This is the write path the provider-settings UI already calls. It is financially material: it decides
/// whether customers must pay before a booking can be confirmed. <see cref="ActingUserId"/> is populated server-side
/// from the JWT and enforced by the authorization pipeline so only the owning provider (or an admin) can change it.</para>
///
/// <para>Applies to <b>future bookings only</b> — existing bookings keep their own policy snapshot.</para>
/// </summary>
public sealed record UpdateBookingPreferencesCommand(
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
    Guid ActingUserId,
    Guid? IdempotencyKey = null)
    : ICommand<UpdateBookingPreferencesResult>, IRequireProviderOwnership;
