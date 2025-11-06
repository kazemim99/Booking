// ========================================
// Booksy.UserManagement.Domain/Events/PhoneVerificationExpiredEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationExpiredEvent(
        VerificationId VerificationId,
        string PhoneNumber
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
