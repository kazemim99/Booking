// ========================================
// AsanRezerve.UserManagement.Domain/Events/PhoneVerificationFailedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.UserManagement.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationFailedEvent(
        VerificationId VerificationId,
        string PhoneNumber,
        int FailedAttempts,
        int MaxAttempts
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
