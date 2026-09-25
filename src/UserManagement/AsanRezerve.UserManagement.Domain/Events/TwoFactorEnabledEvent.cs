// ========================================
// AsanRezerve.UserManagement.Domain/Events/TwoFactorEnabledEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record TwoFactorEnabledEvent(
        UserId UserId,
        DateTime EnabledAt
    ) : DomainEvent("User", UserId.ToString());
}
