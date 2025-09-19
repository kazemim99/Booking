// ========================================
// Booksy.UserManagement.Domain/Events/PasswordResetRequestedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record PasswordResetRequestedEvent(
        UserId UserId,
        Email Email,
        string ResetToken,
        DateTime ExpiresAt
    ) : DomainEvent("User", UserId.ToString());
}
