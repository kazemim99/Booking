// ========================================
// Booksy.UserManagement.Domain/Enums/UserStatus.cs
// ========================================


namespace Booksy.UserManagement.Domain.Events
{
    public sealed class PasswordChangedEvent : DomainEvent
    {
        public UserId UserId { get; }
        public DateTime ChangedAt { get; }

        public PasswordChangedEvent(UserId userId, DateTime changedAt)
            : base("User", userId.ToString())
        {
            UserId = userId;
            ChangedAt = changedAt;
        }
    }
}

