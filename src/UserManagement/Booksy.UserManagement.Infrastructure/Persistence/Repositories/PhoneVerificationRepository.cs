using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booksy.UserManagement.Infrastructure.Persistence.Repositories;

public class PhoneVerificationRepository : IPhoneVerificationRepository
{
    private readonly UserManagementDbContext _context;

    public PhoneVerificationRepository(UserManagementDbContext context)
    {
        _context = context;
    }

    public async Task<PhoneVerification?> GetByPhoneNumberAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        return await _context.PhoneVerifications
            .Where(v => v.PhoneNumber == phoneNumber)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PhoneVerification?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.PhoneVerifications
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<PhoneVerification> AddAsync(
        PhoneVerification verification,
        CancellationToken cancellationToken = default)
    {
        await _context.PhoneVerifications.AddAsync(verification, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return verification;
    }

    public async Task UpdateAsync(
        PhoneVerification verification,
        CancellationToken cancellationToken = default)
    {
        _context.PhoneVerifications.Update(verification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        PhoneVerification verification,
        CancellationToken cancellationToken = default)
    {
        _context.PhoneVerifications.Remove(verification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteExpiredAsync(
        DateTime cutoffTime,
        CancellationToken cancellationToken = default)
    {
        return await _context.PhoneVerifications
            .Where(v => v.ExpiresAt < cutoffTime && !v.IsVerified)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<bool> ExistsActiveVerificationAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        return await _context.PhoneVerifications
            .AnyAsync(v => v.PhoneNumber == phoneNumber &&
                          v.ExpiresAt > DateTime.UtcNow &&
                          !v.IsVerified,
                          cancellationToken);
    }
}
