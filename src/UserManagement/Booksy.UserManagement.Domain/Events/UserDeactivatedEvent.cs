// ========================================
// Booksy.UserManagement.Domain/Enums/UserStatus.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed class UserDeactivatedEvent : DomainEvent
    {
        public UserId UserId { get; }
        public string Reason { get; }
        public DateTime DeactivatedAt { get; }

        public UserDeactivatedEvent(UserId userId, string reason, DateTime deactivatedAt)
            : base("User", userId.ToString())
        {
            UserId = userId;
            Reason = reason;
            DeactivatedAt = deactivatedAt;
        }
    }
}


