// ========================================
// AsanRezerve.UserManagement.Domain/Events/PhoneVerificationExpiredEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.UserManagement.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationExpiredEvent(
        VerificationId VerificationId,
        string PhoneNumber
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
