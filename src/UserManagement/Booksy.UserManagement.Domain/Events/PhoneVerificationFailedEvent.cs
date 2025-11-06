// ========================================
// Booksy.UserManagement.Domain/Events/PhoneVerificationFailedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationFailedEvent(
        VerificationId VerificationId,
        string PhoneNumber,
        int FailedAttempts,
        int MaxAttempts
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
