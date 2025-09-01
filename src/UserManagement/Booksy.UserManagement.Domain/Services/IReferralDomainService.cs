// ========================================
// Booksy.UserManagement.Domain/Exceptions/UserManagementDomainException.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Services;

namespace Booksy.UserManagement.Domain.Services
{
    /// <summary>
    /// Domain service for handling user referrals
    /// </summary>
    public interface IReferralDomainService : IDomainService
    {
        /// <summary>
        /// Generates a unique referral code for a user
        /// </summary>
        string GenerateReferralCode(UserId userId);

        /// <summary>
        /// Validates a referral code
        /// </summary>
        Task<bool> IsValidReferralCodeAsync(string referralCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the user ID associated with a referral code
        /// </summary>
        Task<UserId?> GetReferrerIdAsync(string referralCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records a successful referral
        /// </summary>
        Task RecordReferralAsync(UserId referrerId, UserId referredUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets referral statistics for a user
        /// </summary>
        Task<ReferralStatistics> GetReferralStatisticsAsync(UserId userId, CancellationToken cancellationToken = default);
    }


    /// <summary>
    /// Referral statistics for a user
    /// </summary>
    public sealed class ReferralStatistics
    {
        public int TotalReferrals { get; set; }
        public int SuccessfulReferrals { get; set; }
        public int PendingReferrals { get; set; }
        public decimal TotalRewards { get; set; }
        public DateTime? LastReferralDate { get; set; }
    }
}
