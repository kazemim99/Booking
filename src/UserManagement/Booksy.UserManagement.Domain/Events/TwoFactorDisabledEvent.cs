// ========================================
// Booksy.UserManagement.Domain/Enums/UserStatus.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed class TwoFactorDisabledEvent : DomainEvent
    {
        public UserId UserId { get; }
        public DateTime DisabledAt { get; }

        public TwoFactorDisabledEvent(UserId userId, DateTime disabledAt)
            : base("User", userId.ToString())
        {
            UserId = userId;
            DisabledAt = disabledAt;
        }
    }
}