// ========================================
// Booksy.UserManagement.Domain/Events/PhoneVerificationResendEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationResendEvent(
        VerificationId VerificationId,
        string PhoneNumber,
        VerificationMethod Method,
        int ResendAttemptNumber
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
