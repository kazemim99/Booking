
namespace Booksy.UserManagement.Domain.Entities;

/// <summary>
/// Represents a phone number verification attempt for passwordless authentication
/// </summary>
public class PhoneVerification : Entity<Guid>
{
    private PhoneVerification()
    {
        
    }
    public PhoneVerification(
        Guid id,
        string phoneNumber,
        string countryCode,
        string hashedCode,
        DateTime expiresAt) : base(id)
    {
        PhoneNumber = phoneNumber;
        CountryCode = countryCode;
        HashedCode = hashedCode;
        ExpiresAt = expiresAt;
        IsVerified = false;
        AttemptCount = 0;
        MaxAttempts = 3;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Phone number in E.164 format (e.g., +4917012345678)
    /// </summary>
    public string PhoneNumber { get; private set; }

    /// <summary>
    /// Country code (e.g., DE, US, GB)
    /// </summary>
    public string CountryCode { get; private set; }

    /// <summary>
    /// Hashed verification code (SHA256)
    /// </summary>
    public string HashedCode { get; private set; }

    /// <summary>
    /// Expiration time for the verification code (typically 5 minutes)
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Whether the phone number has been successfully verified
    /// </summary>
    public bool IsVerified { get; private set; }

    /// <summary>
    /// Date and time when the phone was verified
    /// </summary>
    public DateTime? VerifiedAt { get; private set; }

    /// <summary>
    /// Number of verification attempts made
    /// </summary>
    public int AttemptCount { get; private set; }

    /// <summary>
    /// Maximum allowed verification attempts
    /// </summary>
    public int MaxAttempts { get; private set; }

    /// <summary>
    /// IP address of the requester (for security logging)
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent of the requester
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    // ========================================
    // Business Logic Methods
    // ========================================

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    public bool IsMaxAttemptsReached() => AttemptCount >= MaxAttempts;

    public void IncrementAttemptCount()
    {
        AttemptCount++;
    }

    public void MarkAsVerified()
    {
        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
    }

    public void SetRequestInfo(string? ipAddress, string? userAgent)
    {
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public int GetRemainingAttempts() => MaxAttempts - AttemptCount;

    /// <summary>
    /// Factory method for test number verification (auto-pass)
    /// </summary>
    public static PhoneVerification CreateTestVerification(string phoneNumber, string countryCode)
    {
        return new PhoneVerification(
            Guid.NewGuid(),
            phoneNumber,
            countryCode,
            "test-hash",
            DateTime.UtcNow.AddMinutes(5)
        );
    }
}
