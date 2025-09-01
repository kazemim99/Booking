// ========================================
// Booksy.UserManagement.Domain/Enums/UserStatus.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed class UserAuthenticatedEvent : DomainEvent
    {
        public UserId UserId { get; }
        public Email Email { get; }
        public DateTime AuthenticatedAt { get; }
        public string? IpAddress { get; }
        public string? UserAgent { get; }

        public UserAuthenticatedEvent(
            UserId userId,
            Email email,
            DateTime authenticatedAt,
            string? ipAddress = null,
            string? userAgent = null)
            : base("User", userId.ToString())
        {
            UserId = userId;
            Email = email;
            AuthenticatedAt = authenticatedAt;
            IpAddress = ipAddress;
            UserAgent = userAgent;
        }
    }
}


