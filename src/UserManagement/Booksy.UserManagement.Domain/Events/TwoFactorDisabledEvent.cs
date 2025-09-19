// ========================================
// Booksy.UserManagement.Domain/Events/TwoFactorDisabledEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record TwoFactorDisabledEvent(
        UserId UserId,
        DateTime DisabledAt
    ) : DomainEvent("User", UserId.ToString());
}
