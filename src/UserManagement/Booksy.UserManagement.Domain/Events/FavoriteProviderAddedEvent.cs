// ========================================
// Booksy.UserManagement.Domain/Events/FavoriteProviderAddedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record FavoriteProviderAddedEvent(
        CustomerId CustomerId,
        Guid ProviderId,
        DateTime AddedAt
    ) : DomainEvent("Customer", CustomerId.ToString());
}
