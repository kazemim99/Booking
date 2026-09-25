// ========================================
// AsanRezerve.UserManagement.Domain/Events/PhoneVerificationResendEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.UserManagement.Domain.Enums;
using AsanRezerve.UserManagement.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationResendEvent(
        VerificationId VerificationId,
        string PhoneNumber,
        VerificationMethod Method,
        int ResendAttemptNumber
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
