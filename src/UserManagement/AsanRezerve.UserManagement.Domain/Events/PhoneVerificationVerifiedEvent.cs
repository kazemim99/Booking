// ========================================
// AsanRezerve.UserManagement.Domain/Events/PhoneVerificationVerifiedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.UserManagement.Domain.Enums;
using AsanRezerve.UserManagement.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationVerifiedEvent(
        VerificationId VerificationId,
        string PhoneNumber,
        Guid? UserId,
        VerificationPurpose Purpose,
        int TotalAttempts
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
