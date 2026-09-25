// ========================================
// AsanRezerve.UserManagement.Domain/Events/PhoneVerificationSentEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.UserManagement.Domain.Enums;
using AsanRezerve.UserManagement.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationSentEvent(
        VerificationId VerificationId,
        string PhoneNumber,
        VerificationMethod Method,
        int SendAttemptNumber
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
