// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/PayoutCompletedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record PayoutCompletedEvent(
        PayoutId PayoutId,
        ProviderId ProviderId,
        Money Amount, // net amount paid to the provider
        DateTime PaidAt,
        Money? GrossAmount = null,       // for ledger commission recognition (Gross = Net + Commission)
        Money? CommissionAmount = null) : DomainEvent;
}
