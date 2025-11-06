// ========================================
// Booksy.UserManagement.Application/Commands/PhoneVerification/RequestVerification/RequestPhoneVerificationCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Application.Commands.PhoneVerification.RequestVerification
{
    /// <summary>
    /// Command to request phone number verification with OTP
    /// </summary>
    public sealed record RequestPhoneVerificationCommand(
        string PhoneNumber,
        VerificationPurpose Purpose,
        VerificationMethod Method = VerificationMethod.Sms,
        Guid? UserId = null,
        string? IpAddress = null,
        string? UserAgent = null,
        string? SessionId = null
    ) : ICommand<RequestPhoneVerificationResult>;
}
