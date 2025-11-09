// ========================================
// Booksy.UserManagement.Domain/Aggregates/PhoneVerificationAggregate/PhoneVerification.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Events;

namespace Booksy.UserManagement.Domain.Aggregates.PhoneVerificationAggregate
{
    /// <summary>
    /// PhoneVerification aggregate root for managing OTP-based phone verification
    /// </summary>
    public sealed class PhoneVerification : AggregateRoot<VerificationId>
    {
        // Identity
        public UserId? UserId { get; private set; }
        public PhoneNumber PhoneNumber { get; private set; }

        // OTP Details
        public OtpCode OtpCode { get; private set; }
        public string OtpHash { get; private set; } // Hashed OTP for security

        // Verification Details
        public VerificationStatus Status { get; private set; }
        public VerificationMethod Method { get; private set; }
        public VerificationPurpose Purpose { get; private set; }

        // Attempt Tracking
        public int SendAttempts { get; private set; }
        public int VerificationAttempts { get; private set; }
        public int MaxVerificationAttempts { get; private set; }
        public DateTime? LastSentAt { get; private set; }
        public DateTime? LastAttemptAt { get; private set; }

        // Lifecycle Timestamps
        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime? VerifiedAt { get; private set; }
        public DateTime? BlockedUntil { get; private set; }

        // Metadata
        public string? IpAddress { get; private set; }
        public string? UserAgent { get; private set; }
        public string? SessionId { get; private set; }

        // EF Core constructor
        private PhoneVerification() : base(VerificationId.Create())
        {
            PhoneNumber = null!;
            OtpCode = null!;
            OtpHash = string.Empty;
        }

        private PhoneVerification(
            PhoneNumber phoneNumber,
            VerificationMethod method,
            VerificationPurpose purpose,
            UserId? userId = null,
            int maxAttempts = 5) : base(VerificationId.Create())
        {
            PhoneNumber = phoneNumber;
            Method = method;
            Purpose = purpose;
            UserId = userId;
            Status = VerificationStatus.Pending;
            MaxVerificationAttempts = maxAttempts;
            SendAttempts = 0;
            VerificationAttempts = 0;
            CreatedAt = DateTime.UtcNow;

            // Generate OTP
            OtpCode = OtpCode.Generate(6, 5); // 6 digits, 5 minutes validity
            OtpHash = HashOtp(OtpCode.Value);
            ExpiresAt = OtpCode.ExpiresAt;

            RaiseDomainEvent(new PhoneVerificationRequestedEvent(
                Id,
                phoneNumber.Value,
                method,
                purpose,
                userId?.Value));
        }

        public static PhoneVerification Create(
            PhoneNumber phoneNumber,
            VerificationMethod method,
            VerificationPurpose purpose,
            UserId? userId = null,
            int maxAttempts = 5)
        {
            if (maxAttempts < 3 || maxAttempts > 10)
                throw new ArgumentException("Max attempts must be between 3 and 10", nameof(maxAttempts));

            return new PhoneVerification(phoneNumber, method, purpose, userId, maxAttempts);
        }

        public void SetMetadata(string? ipAddress, string? userAgent, string? sessionId)
        {
            IpAddress = ipAddress;
            UserAgent = userAgent;
            SessionId = sessionId;
        }

        public void MarkAsSent()
        {
            EnsureValidState(() => Status == VerificationStatus.Pending, "MarkAsSent", Status.ToString());

            Status = VerificationStatus.Sent;
            SendAttempts++;
            LastSentAt = DateTime.UtcNow;

            RaiseDomainEvent(new PhoneVerificationSentEvent(
                Id,
                PhoneNumber.Value,
                Method,
                SendAttempts));
        }

        public bool CanResend()
        {
            // Can resend if:
            // 1. Status is Sent or Failed
            // 2. Not verified
            // 3. Not blocked
            // 4. Not expired
            // 5. At least 60 seconds since last send
            // 6. Less than 3 send attempts

            if (Status == VerificationStatus.Verified)
                return false;

            if (Status == VerificationStatus.Blocked)
                return false;

            if (IsExpired())
                return false;

            if (SendAttempts >= 3)
                return false;

            if (LastSentAt.HasValue && (DateTime.UtcNow - LastSentAt.Value).TotalSeconds < 60)
                return false;

            return true;
        }

        public void Resend()
        {
            if (!CanResend())
                throw new InvalidOperationException("Cannot resend OTP at this time");

            // Generate new OTP
            OtpCode = OtpCode.Generate(6, 5);
            OtpHash = HashOtp(OtpCode.Value);
            ExpiresAt = OtpCode.ExpiresAt;

            Status = VerificationStatus.Pending;
            VerificationAttempts = 0; // Reset verification attempts on resend

            RaiseDomainEvent(new PhoneVerificationResendEvent(
                Id,
                PhoneNumber.Value,
                Method,
                SendAttempts + 1));
        }

        public bool Verify(string inputCode)
        {
            // Check if verification is possible
            if (Status == VerificationStatus.Verified)
                throw new InvalidOperationException("Phone number already verified");

            if (Status == VerificationStatus.Blocked)
                throw new InvalidOperationException("Verification is blocked due to too many failed attempts");

            if (IsExpired())
            {
                Status = VerificationStatus.Expired;
                RaiseDomainEvent(new PhoneVerificationExpiredEvent(Id, PhoneNumber.Value));
                throw new InvalidOperationException("OTP has expired");
            }

            // Record attempt
            VerificationAttempts++;
            LastAttemptAt = DateTime.UtcNow;

            // Verify OTP by comparing hashes (OtpCode is not persisted, so we hash the input and compare)
            var inputHash = HashOtp(inputCode);
            var isValid = inputHash.Equals(OtpHash, StringComparison.Ordinal);

            if (isValid)
            {
                Status = VerificationStatus.Verified;
                VerifiedAt = DateTime.UtcNow;

                RaiseDomainEvent(new PhoneVerificationVerifiedEvent(
                    Id,
                    PhoneNumber.Value,
                    UserId?.Value,
                    Purpose,
                    VerificationAttempts));

                return true;
            }
            else
            {
                // Failed attempt
                if (VerificationAttempts >= MaxVerificationAttempts)
                {
                    // Block for 1 hour
                    Status = VerificationStatus.Blocked;
                    BlockedUntil = DateTime.UtcNow.AddHours(1);

                    RaiseDomainEvent(new PhoneVerificationBlockedEvent(
                        Id,
                        PhoneNumber.Value,
                        VerificationAttempts,
                        BlockedUntil.Value));
                }
                else
                {
                    Status = VerificationStatus.Failed;

                    RaiseDomainEvent(new PhoneVerificationFailedEvent(
                        Id,
                        PhoneNumber.Value,
                        VerificationAttempts,
                        MaxVerificationAttempts));
                }

                return false;
            }
        }

        public void Cancel(string reason = "Cancelled by user")
        {
            if (Status == VerificationStatus.Verified)
                throw new InvalidOperationException("Cannot cancel verified phone number");

            Status = VerificationStatus.Cancelled;

            RaiseDomainEvent(new PhoneVerificationCancelledEvent(
                Id,
                PhoneNumber.Value,
                reason));
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow > ExpiresAt;
        }

        public bool IsBlocked()
        {
            if (!BlockedUntil.HasValue)
                return false;

            if (DateTime.UtcNow > BlockedUntil.Value)
            {
                // Unblock automatically
                BlockedUntil = null;
                return false;
            }

            return true;
        }

        public TimeSpan RemainingValidity()
        {
            if (IsExpired())
                return TimeSpan.Zero;

            return ExpiresAt - DateTime.UtcNow;
        }

        public int RemainingAttempts()
        {
            return Math.Max(0, MaxVerificationAttempts - VerificationAttempts);
        }

        private static string HashOtp(string otp)
        {
            // Simple hash for demonstration - in production use proper hashing (SHA256, etc.)
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(otp);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
