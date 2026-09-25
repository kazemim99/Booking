// ========================================
// AsanRezerve.UserManagement.Domain/Events/FavoriteProviderRemovedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record FavoriteProviderRemovedEvent(
        CustomerId CustomerId,
        Guid ProviderId,
        DateTime RemovedAt
    ) : DomainEvent("Customer", CustomerId.ToString());
}
