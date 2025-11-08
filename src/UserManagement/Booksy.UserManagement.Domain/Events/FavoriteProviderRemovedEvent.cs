// ========================================
// Booksy.UserManagement.Domain/Events/FavoriteProviderRemovedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record FavoriteProviderRemovedEvent(
        CustomerId CustomerId,
        Guid ProviderId,
        DateTime RemovedAt
    ) : DomainEvent("Customer", CustomerId.ToString());
}
