using Booksy.UserManagement.Domain.Entities;

namespace Booksy.UserManagement.Application.Services.Interfaces;

public interface IPhoneVerificationService
{
    /// <summary>
    /// Generates and sends OTP code to phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number in E.164 format</param>
    /// <param name="countryCode">Country code (e.g., DE, US)</param>
    /// <param name="ipAddress">IP address for security logging</param>
    /// <param name="userAgent">User agent for security logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification entity with masked phone for display</returns>
    Task<(PhoneVerification Verification, string MaskedPhone, int ExpiresInSeconds)> SendVerificationCodeAsync(
        string phoneNumber,
        string countryCode,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies OTP code for phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number in E.164 format</param>
    /// <param name="code">6-digit OTP code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result with remaining attempts</returns>
    Task<(bool IsValid, int RemainingAttempts, string? ErrorMessage)> VerifyCodeAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Masks phone number for display (e.g., +49 170 ••• ••78)
    /// </summary>
    string MaskPhoneNumber(string phoneNumber);

    /// <summary>
    /// Cleans up expired verifications (background job)
    /// </summary>
    Task<int> CleanupExpiredVerificationsAsync(CancellationToken cancellationToken = default);
}
