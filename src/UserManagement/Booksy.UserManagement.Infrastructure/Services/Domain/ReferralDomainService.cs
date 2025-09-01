
// Booksy.UserManagement.Infrastructure/Services/Domain/ReferralDomainService.cs
using Booksy.UserManagement.Domain.Services;
using Booksy.UserManagement.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Memory;

namespace Booksy.UserManagement.Infrastructure.Services.Domain
{
    public class ReferralDomainService : IReferralDomainService
    {
        private readonly IMemoryCache _cache;

        public ReferralDomainService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public string GenerateReferralCode(UserId userId)
        {
            var code = $"REF{userId.Value.ToString("N")[..8].ToUpper()}";
            _cache.Set($"referral:code:{code}", userId.Value, TimeSpan.FromDays(365));
            return code;
        }

        public async Task<bool> IsValidReferralCodeAsync(string referralCode, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_cache.TryGetValue($"referral:code:{referralCode}", out _));
        }

        public async Task<UserId?> GetReferrerIdAsync(string referralCode, CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue<Guid>($"referral:code:{referralCode}", out var referrerId))
            {
                return UserId.From(referrerId);
            }
            return await Task.FromResult<UserId?>(null);
        }

        public async Task RecordReferralAsync(UserId referrerId, UserId referredUserId, CancellationToken cancellationToken = default)
        {
            var key = $"referrals:{referrerId.Value}";
            var referrals = _cache.Get<List<Guid>>(key) ?? new List<Guid>();
            referrals.Add(referredUserId.Value);
            _cache.Set(key, referrals, TimeSpan.FromDays(30));
            await Task.CompletedTask;
        }

        public async Task<ReferralStatistics> GetReferralStatisticsAsync(UserId userId, CancellationToken cancellationToken = default)
        {
            var key = $"referrals:{userId.Value}";
            var referrals = _cache.Get<List<Guid>>(key) ?? new List<Guid>();

            return await Task.FromResult(new ReferralStatistics
            {
                TotalReferrals = referrals.Count,
                SuccessfulReferrals = referrals.Count, // In production, would check actual activation
                PendingReferrals = 0,
                TotalRewards = referrals.Count * 10m, // Example: $10 per referral
                LastReferralDate = referrals.Any() ? DateTime.UtcNow : null
            });
        }
    }
}