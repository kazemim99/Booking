// ========================================
// Booksy.ServiceCatalog.Domain/Events/PayoutCompletedEvent.cs
// ========================================
using Booksy.Core.Domain.Abstractions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record PayoutCompletedEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        Money Amount, // net amount paid to the provider
        DateTime PaidAt,
        Money? GrossAmount = null,       // for ledger commission recognition (Gross = Net + Commission)
        Money? CommissionAmount = null) : DomainEvent;
}
