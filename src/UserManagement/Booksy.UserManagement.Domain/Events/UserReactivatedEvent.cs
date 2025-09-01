// ========================================
// Booksy.UserManagement.Domain/Enums/UserStatus.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed class UserReactivatedEvent : DomainEvent
    {
        public UserId UserId { get; }
        public DateTime ReactivatedAt { get; }

        public UserReactivatedEvent(UserId userId, DateTime reactivatedAt)
            : base("User", userId.ToString())
        {
            UserId = userId;
            ReactivatedAt = reactivatedAt;
        }
    }
}


