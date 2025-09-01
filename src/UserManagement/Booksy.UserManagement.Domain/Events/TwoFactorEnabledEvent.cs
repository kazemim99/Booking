// ========================================
// Booksy.UserManagement.Domain/Enums/UserStatus.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed class TwoFactorEnabledEvent : DomainEvent
    {
        public UserId UserId { get; }
        public DateTime EnabledAt { get; }

        public TwoFactorEnabledEvent(UserId userId, DateTime enabledAt)
            : base("User", userId.ToString())
        {
            UserId = userId;
            EnabledAt = enabledAt;
        }
    }
}


