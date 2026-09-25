// ========================================
// AsanRezerve.UserManagement.Domain/Events/PhoneVerificationRequestedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.UserManagement.Domain.Enums;
using AsanRezerve.UserManagement.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationRequestedEvent(
        VerificationId VerificationId,
        string PhoneNumber,
        VerificationMethod Method,
        VerificationPurpose Purpose,
        Guid? UserId
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
