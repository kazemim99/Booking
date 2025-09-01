// ========================================
// Security Services
// ========================================

// Booksy.UserManagement.Infrastructure/Services/Security/PasswordHasher.cs
using Booksy.UserManagement.Application.Services.Interfaces;
using Booksy.UserManagement.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Booksy.UserManagement.Infrastructure.Services.Domain
{
    public class ReferralService : IReferralService
    {
        private readonly IMemoryCache _cache;

        public ReferralService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<Guid?> GetReferrerIdAsync(string referralCode, CancellationToken cancellationToken = default)
        {
            // In production, this would query a database
            // For now, using in-memory cache as a simple implementation
            if (_cache.TryGetValue<Guid>($"referral:{referralCode}", out var referrerId))
            {
                return referrerId;
            }

            return await Task.FromResult<Guid?>(null);
        }

        public async Task RecordReferralAsync(Guid referrerId, UserId referredUserId, CancellationToken cancellationToken = default)
        {
            // In production, this would save to database
            var key = $"referrals:{referrerId}";
            var referrals = _cache.Get<List<Guid>>(key) ?? new List<Guid>();
            referrals.Add(referredUserId.Value);
            _cache.Set(key, referrals, TimeSpan.FromDays(30));

            await Task.CompletedTask;
        }

        public async Task<string> GenerateReferralCodeAsync(UserId userId, CancellationToken cancellationToken = default)
        {
            // Generate a unique referral code
            var code = $"REF{userId.Value.ToString("N")[..8].ToUpper()}";

            // Store the mapping
            _cache.Set($"referral:{code}", userId.Value, TimeSpan.FromDays(365));

            return await Task.FromResult(code);
        }

        public async Task<int> GetReferralCountAsync(UserId userId, CancellationToken cancellationToken = default)
        {
            var key = $"referrals:{userId.Value}";
            var referrals = _cache.Get<List<Guid>>(key) ?? new List<Guid>();
            return await Task.FromResult(referrals.Count);
        }
    }
}


