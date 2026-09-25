// ========================================
// AsanRezerve.UserManagement.Domain/Events/TwoFactorDisabledEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record TwoFactorDisabledEvent(
        UserId UserId,
        DateTime DisabledAt
    ) : DomainEvent("User", UserId.ToString());
}
