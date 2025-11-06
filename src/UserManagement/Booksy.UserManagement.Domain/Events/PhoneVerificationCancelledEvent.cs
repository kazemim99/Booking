// ========================================
// Booksy.UserManagement.Domain/Events/PhoneVerificationCancelledEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationCancelledEvent(
        VerificationId VerificationId,
        string PhoneNumber,
        string Reason
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
