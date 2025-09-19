// ========================================
// Booksy.UserManagement.Domain/Events/UserAuthenticatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record UserAuthenticatedEvent(
        UserId UserId,
        Email Email,
        DateTime AuthenticatedAt,
        string? IpAddress = null,
        string? UserAgent = null
    ) : DomainEvent("User", UserId.ToString());
}
