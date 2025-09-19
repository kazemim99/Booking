using Booksy.Core.Application.Abstractions.Events;

namespace Booksy.UserManagement.Application.Abstractions.Events
{
    public sealed record UserDeactivatedIntegrationEvent : IntegrationEvent
    {
        public Guid UserId { get; }
        public string Email { get; }
        public string Reason { get; }
        public DateTime DeactivatedAt { get; }

        public UserDeactivatedIntegrationEvent(
            Guid userId,
            string email,
            string reason,
            DateTime deactivatedAt) : base("UserManagement")
        {
            UserId = userId;
            Email = email;
            Reason = reason;
            DeactivatedAt = deactivatedAt;
        }
    }
}
