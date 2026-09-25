// ========================================
// AsanRezerve.UserManagement.Domain/Events/PhoneVerificationCancelledEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.UserManagement.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationCancelledEvent(
        VerificationId VerificationId,
        string PhoneNumber,
        string Reason
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
