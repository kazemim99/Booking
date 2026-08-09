// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Repositories/PhoneVerificationRepository.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.UserManagement.Domain.Aggregates.PhoneVerificationAggregate;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.ValueObjects;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booksy.UserManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for PhoneVerification aggregate
/// </summary>
public class PhoneVerificationRepository
    : EfRepositoryBase<PhoneVerification, VerificationId, UserManagementDbContext>,
      IPhoneVerificationRepository
{
    public PhoneVerificationRepository(UserManagementDbContext context) : base(context)
    {
    }

    public override async Task<PhoneVerification?> GetByIdAsync(
        VerificationId id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<PhoneVerification?> GetByPhoneAndPurposeAsync(
        string phoneNumber,
        VerificationPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(v => v.PhoneNumber.Value == phoneNumber && v.Purpose == purpose)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PhoneVerification?> GetByPhoneNumberAsync(
        PhoneNumber phoneNumber,
        CancellationToken cancellationToken = default)
    {
        // Match every stored representation, not just the canonical one: rows
        // written before phone numbers were canonicalized hold the caller's raw
        // format, and missing them resurfaces as "No verification found".
        var forms = phoneNumber.EquivalentForms();

        // Return the newest still-actionable verification. Terminal records
        // (already Verified, or Cancelled) must be excluded: otherwise a
        // just-verified record shadows the fresh OTP from a new send and
        // re-login fails with "already verified".
        return await DbSet
            .Where(v => (forms.Contains(v.PhoneNumber.Value)
                         || forms.Contains(v.PhoneNumber.NationalNumber))
                        && v.Status != VerificationStatus.Verified
                        && v.Status != VerificationStatus.Cancelled
                        && v.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<PhoneVerification>> GetRecentVerificationsByPhoneAsync(
        string phoneNumber,
        TimeSpan timeWindow,
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow - timeWindow;

        return await DbSet
            .Where(v => v.PhoneNumber.Value == phoneNumber && v.CreatedAt >= cutoffTime)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PhoneVerification>> GetExpiredVerificationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(v => v.ExpiresAt < DateTime.UtcNow &&
                       v.Status != VerificationStatus.Verified &&
                       v.Status != VerificationStatus.Cancelled)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        PhoneVerification verification,
        CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(verification, cancellationToken);
    }

    public new async Task UpdateAsync(
        PhoneVerification verification,
        CancellationToken cancellationToken = default)
    {
        DbSet.Update(verification);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(
        PhoneVerification verification,
        CancellationToken cancellationToken = default)
    {
        DbSet.Remove(verification);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsActiveVerificationAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(v => v.PhoneNumber.Value == phoneNumber &&
                          v.ExpiresAt > DateTime.UtcNow &&
                          v.Status == VerificationStatus.Sent,
                          cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }
}
