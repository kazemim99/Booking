// ========================================
// Booksy.UserManagement.Domain/Enums/UserStatus.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed class UserRegisteredEvent : DomainEvent
    {
        public UserId UserId { get; }
        public Email Email { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public UserType UserType { get; }
        public DateTime RegisteredAt { get; }

        public UserRegisteredEvent(
            UserId userId,
            Email email,
            string firstName,
            string lastName,
            UserType userType,
            DateTime registeredAt)
            : base("User", userId.ToString())
        {
            UserId = userId;
            Email = email;
            FirstName = firstName;
            LastName = lastName;
            UserType = userType;
            RegisteredAt = registeredAt;
        }
    }
}

