// ========================================
// Booksy.UserManagement.Domain/Events/PhoneVerificationSentEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationSentEvent(
        VerificationId VerificationId,
        string PhoneNumber,
        VerificationMethod Method,
        int SendAttemptNumber
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
