using Booksy.Core.Domain.Abstractions.Events;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    /// <summary>
    /// Raised when a provider's default booking policy changes.
    ///
    /// <para>This is the <b>audit record</b> for a financially material change: it decides whether customers must pay
    /// a deposit before their booking can be confirmed, and how much. Both the previous and new deposit terms are
    /// captured (previous values are null when no policy existed) so a change can be reconstructed after the fact,
    /// together with who made it and when.</para>
    /// </summary>
    public sealed record ProviderBookingPolicyChangedEvent(
        ProviderId ProviderId,
        UserId? ChangedBy,
        bool? PreviousRequireDeposit,
        string? PreviousDepositType,
        decimal? PreviousDepositPercentage,
        decimal? PreviousDepositFixedAmount,
        bool RequireDeposit,
        string DepositType,
        decimal DepositPercentage,
        decimal DepositFixedAmount,
        DateTime ChangedAt) : DomainEvent;
}
