// ========================================
// Booksy.UserManagement.Domain/Enums/UserStatus.cs
// ========================================

using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed class UserActivatedEvent : DomainEvent
    {
        public UserId UserId { get; }
        public Email Email { get; }
        public DateTime ActivatedAt { get; }

        public UserActivatedEvent(UserId userId, Email email, DateTime activatedAt)
            : base("User", userId.ToString())
        {
            UserId = userId;
            Email = email;
            ActivatedAt = activatedAt;
        }
    }
}


