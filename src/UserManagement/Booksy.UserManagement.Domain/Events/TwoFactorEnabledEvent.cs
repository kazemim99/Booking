// ========================================
// Booksy.UserManagement.Domain/Events/TwoFactorEnabledEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record TwoFactorEnabledEvent(
        UserId UserId,
        DateTime EnabledAt
    ) : DomainEvent("User", UserId.ToString());
}
