// ========================================
// AsanRezerve.UserManagement.Domain/Events/PasswordResetRequestedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record PasswordResetRequestedEvent(
        UserId UserId,
        Email Email,
        string ResetToken,
        DateTime ExpiresAt
    ) : DomainEvent("User", UserId.ToString());
}
