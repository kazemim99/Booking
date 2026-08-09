using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBookingPreferences;

/// <summary>
/// Sets the provider's default booking policy (P0-0 — the write path that makes deposits configurable at all).
///
/// <para>Authorization is enforced upstream by the pipeline's ownership check, so by the time this runs the caller is
/// the provider's owner or an admin. The domain performs the final validation and raises
/// <c>ProviderBookingPolicyChangedEvent</c> as the audit record.</para>
///
/// <para>Only future bookings are affected: each booking snapshots the policy in force when it was created.</para>
/// </summary>
public sealed class UpdateBookingPreferencesCommandHandler
    : ICommandHandler<UpdateBookingPreferencesCommand, UpdateBookingPreferencesResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly ILogger<UpdateBookingPreferencesCommandHandler> _logger;

    public UpdateBookingPreferencesCommandHandler(
        IProviderWriteRepository providerRepository,
        ILogger<UpdateBookingPreferencesCommandHandler> logger)
    {
        _providerRepository = providerRepository;
        _logger = logger;
    }

    public async Task<UpdateBookingPreferencesResult> Handle(
        UpdateBookingPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        var provider = await _providerRepository.GetByIdAsync(
            ProviderId.From(command.ProviderId), cancellationToken);

        if (provider is null)
            throw new DomainValidationException("Provider not found");

        var policy = BookingPolicy.Create(
            minAdvanceBookingHours: command.MinAdvanceBookingHours,
            maxAdvanceBookingDays: command.MaxAdvanceBookingDays,
            cancellationWindowHours: command.CancellationWindowHours,
            cancellationFeePercentage: command.CancellationFeePercentage,
            allowRescheduling: command.AllowRescheduling,
            rescheduleWindowHours: command.RescheduleWindowHours,
            requireDeposit: command.RequireDeposit,
            depositPercentage: command.DepositPercentage,
            depositType: command.DepositType,
            depositFixedAmount: command.DepositFixedAmount);

        provider.SetBookingPolicy(policy, UserId.From(command.ActingUserId));

        await _providerRepository.UpdateAsync(provider, cancellationToken);

        // Financially material change → log it explicitly alongside the domain event.
        _logger.LogWarning(
            "Provider {ProviderId} booking policy changed by {ActingUserId}: RequireDeposit={RequireDeposit}, " +
            "DepositType={DepositType}, Percentage={Percentage}, FixedAmount={FixedAmount}",
            command.ProviderId, command.ActingUserId, policy.RequireDeposit,
            policy.DepositType, policy.DepositPercentage, policy.DepositFixedAmount);

        return new UpdateBookingPreferencesResult(
            command.ProviderId,
            policy.RequireDeposit,
            policy.DepositType,
            policy.DepositPercentage,
            policy.DepositFixedAmount,
            policy.MinAdvanceBookingHours,
            policy.MaxAdvanceBookingDays,
            policy.CancellationWindowHours,
            policy.CancellationFeePercentage,
            policy.AllowRescheduling,
            policy.RescheduleWindowHours,
            DateTime.UtcNow);
    }
}
