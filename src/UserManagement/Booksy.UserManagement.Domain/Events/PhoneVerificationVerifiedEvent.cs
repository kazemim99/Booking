// ========================================
// Booksy.UserManagement.Domain/Events/PhoneVerificationVerifiedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record PhoneVerificationVerifiedEvent(
        VerificationId VerificationId,
        string PhoneNumber,
        Guid? UserId,
        VerificationPurpose Purpose,
        int TotalAttempts
    ) : DomainEvent("PhoneVerification", VerificationId.Value.ToString());
}
