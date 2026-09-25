// ========================================
// AsanRezerve.UserManagement.Domain/Events/UserAuthenticatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record UserAuthenticatedEvent(
        UserId UserId,
        Email Email,
        DateTime AuthenticatedAt,
        string? IpAddress = null,
        string? UserAgent = null
    ) : DomainEvent("User", UserId.ToString());
}
