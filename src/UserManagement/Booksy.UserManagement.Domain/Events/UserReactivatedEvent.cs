// ========================================
// Booksy.UserManagement.Domain/Events/UserReactivatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record UserReactivatedEvent(
        UserId UserId,
        DateTime ReactivatedAt
    ) : DomainEvent("User", UserId.ToString());
}
