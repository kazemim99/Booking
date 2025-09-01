// ========================================
// Booksy.UserManagement.Domain/Enums/UserStatus.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed class PasswordResetRequestedEvent : DomainEvent
    {
        public UserId UserId { get; }
        public Email Email { get; }
        public string ResetToken { get; }
        public DateTime ExpiresAt { get; }

        public PasswordResetRequestedEvent(
            UserId userId,
            Email email,
            string resetToken,
            DateTime expiresAt)
            : base("User", userId.ToString())
        {
            UserId = userId;
            Email = email;
            ResetToken = resetToken;
            ExpiresAt = expiresAt;
        }
    }
}


