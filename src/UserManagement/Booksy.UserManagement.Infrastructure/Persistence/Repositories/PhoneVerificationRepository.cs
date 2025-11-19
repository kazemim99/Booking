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
        return await DbSet
            .Where(v => v.PhoneNumber.Value == phoneNumber.Value && v.ExpiresAt > DateTime.UtcNow)
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
