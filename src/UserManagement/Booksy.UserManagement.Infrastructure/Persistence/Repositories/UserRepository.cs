using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Booksy.UserManagement.Infrastructure.Persistence.Repositories;

public class UserRepository : EfRepositoryBase<User, UserId, UserManagementDbContext>, IUserRepository
{
    public UserRepository(UserManagementDbContext context) : base(context) { }

    // ✅ Domain-specific methods
    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        return await DbSet
            .Include(u => u.Profile)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        // "Exists" means found. This was historically inverted (return user == null)
        // and only produced correct behaviour because both IsEmailAvailableAsync and
        // its callers were inverted in turn; those have been corrected alongside this.
        return await DbSet
            .AnyAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    public async Task<bool> ExistsByPhoneNumberAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);

        // Match every stored representation (canonical "+98…", bare national,
        // legacy local "09…") for the same reason GetByPhoneNumberAsync does.
        // Soft-deleted users are excluded by the DbContext query filter, so a
        // deleted person's phone does not block a fresh registration.
        var forms = phoneNumber.EquivalentForms();

        return await DbSet
            .AnyAsync(u => u.PhoneNumber != null &&
                (forms.Contains(u.PhoneNumber.NationalNumber) ||
                 forms.Contains(u.PhoneNumber.Value)),
                cancellationToken);
    }

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);

        // Match every representation the number may be stored as. Rows written
        // before phone numbers were canonicalized keep the caller's raw format
        // (local "09…" from the mobile apps, "+98…" from the E2E scripts); a
        // miss here makes the caller create a duplicate user, which then fails
        // on the unique synthesized-email index with a 500.
        var forms = TryGetEquivalentForms(phoneNumber);

        return await DbSet
            .Include(u => u.Profile)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.PhoneNumber != null &&
                (forms.Contains(u.PhoneNumber.NationalNumber) ||
                 forms.Contains(u.PhoneNumber.Value)),
                cancellationToken);
    }

    /// <summary>
    /// Equivalent string forms of <paramref name="phoneNumber"/>, falling back
    /// to the raw input when it cannot be parsed (so an unparseable number still
    /// performs an exact-match lookup rather than throwing).
    /// </summary>
    private static IReadOnlyCollection<string> TryGetEquivalentForms(string phoneNumber)
    {
        try
        {
            return Core.Domain.ValueObjects.PhoneNumber.From(phoneNumber).EquivalentForms();
        }
        catch (ArgumentException)
        {
            return new[] { phoneNumber };
        }
    }

    // ✅ Enhanced GetByIdAsync with includes for domain operations
    public override async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.Profile)
            .Include(u => u.Roles)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    // ✅ Business logic in save
    public override async Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var entry = Context.Entry(user);

        if (entry.State == EntityState.Detached)
        {
            await DbSet.AddAsync(user, cancellationToken);
            return;
        }

        // A brand-new aggregate is already pending as an INSERT; leave it alone.
        // DbSet.Update would flip it Added -> Modified (User.Id is domain-assigned,
        // so EF does not treat the key as unset), and the INSERT is then never
        // emitted -- while children added to it in the same request still are, and
        // fail on their FK to a row that was never written. The OTP sign-in path
        // does exactly this: provisioning creates + saves the person, then the
        // handler adds a refresh token and saves the same aggregate again.
        if (entry.State == EntityState.Added)
        {
            return;
        }

        DbSet.Update(user);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        await Task.FromResult(DbSet.Update(user));
    }

    public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await DbSet.Include(c=>c.Profile).Include(c=>c.Roles).ToListAsync();
    }
}