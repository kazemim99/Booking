// ========================================
// Booksy.UserManagement.Domain/Events/PhoneVerificationBlockedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationBlockedEvent(
        VerificationId VerificationId,
        string PhoneNumber,
        int FailedAttempts,
        DateTime BlockedUntil
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
