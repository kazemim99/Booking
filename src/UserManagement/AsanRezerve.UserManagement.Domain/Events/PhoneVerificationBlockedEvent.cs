// ========================================
// AsanRezerve.UserManagement.Domain/Events/PhoneVerificationBlockedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.UserManagement.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationBlockedEvent(
        VerificationId VerificationId,
        string PhoneNumber,
        int FailedAttempts,
        DateTime BlockedUntil
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
