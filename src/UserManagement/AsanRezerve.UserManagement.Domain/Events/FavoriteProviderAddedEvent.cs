// ========================================
// AsanRezerve.UserManagement.Domain/Events/FavoriteProviderAddedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record FavoriteProviderAddedEvent(
        CustomerId CustomerId,
        Guid ProviderId,
        DateTime AddedAt
    ) : DomainEvent("Customer", CustomerId.ToString());
}
