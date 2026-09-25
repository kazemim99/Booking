// ========================================
// Event Handlers - Domain Events
// ========================================

// AsanRezerve.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs

// ========================================
// Event Handlers - Domain Events
// ========================================

// AsanRezerve.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
using AsanRezerve.Core.Application.Abstractions.Events;

namespace AsanRezerve.UserManagement.Application.Abstractions.Events
{
    public sealed record UserCreatedIntegrationEvent : IntegrationEvent
    {
        public Guid UserId { get; }
        public string Email { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string UserType { get; }
        public DateTime CreatedAt { get; }

        public UserCreatedIntegrationEvent(
            Guid userId,
            string email,
            string firstName,
            string lastName,
            string userType,
            DateTime createdAt) : base("UserManagement")
        {
            UserId = userId;
            Email = email;
            FirstName = firstName;
            LastName = lastName;
            UserType = userType;
            CreatedAt = createdAt;
        }
    }
}

