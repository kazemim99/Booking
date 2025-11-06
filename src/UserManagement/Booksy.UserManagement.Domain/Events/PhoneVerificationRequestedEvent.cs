// ========================================
// Booksy.UserManagement.Domain/Events/PhoneVerificationRequestedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationRequestedEvent(
        VerificationId VerificationId,
        string PhoneNumber,
        VerificationMethod Method,
        VerificationPurpose Purpose,
        Guid? UserId
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
