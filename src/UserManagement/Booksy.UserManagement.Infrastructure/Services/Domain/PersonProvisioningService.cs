using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.Services;
using Booksy.UserManagement.Domain.ValueObjects;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Infrastructure.Services.Domain
{
    /// <inheritdoc cref="IPersonProvisioningService"/>
    public sealed class PersonProvisioningService : IPersonProvisioningService
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManagementDbContext _dbContext;
        private readonly ILogger<PersonProvisioningService> _logger;

        public PersonProvisioningService(
            IUserRepository userRepository,
            UserManagementDbContext dbContext,
            ILogger<PersonProvisioningService> logger)
        {
            _userRepository = userRepository;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<PersonProvisioningResult> GetOrCreateByPhoneAsync(
            PhoneNumber phoneNumber,
            UserType capacity,
            string? firstName = null,
            string? lastName = null,
            string? email = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(phoneNumber);

            // PhoneNumber is canonical E.164 by construction; the repository also
            // matches legacy non-canonical stored forms via EquivalentForms().
            var existing = await _userRepository.GetByPhoneNumberAsync(
                phoneNumber.Value, cancellationToken);

            if (existing is not null)
            {
                return await ReuseAsync(existing, capacity, cancellationToken);
            }

            // Nothing found. Two requests for the same brand-new phone (a customer sign-in and
            // a provider sign-in racing, say) both reach this point having both seen "no row",
            // and without the users(phone_number) unique index (§1.2, blocked on production
            // data) nothing in the database stops both from inserting. So the CREATE path is
            // serialized per phone: take a transaction-scoped advisory lock keyed on the
            // canonical number, look again under the lock, and only then create. The loser
            // blocks until the winner commits and then finds the winner's row.
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(
                () => CreateUnderPhoneLockAsync(phoneNumber, capacity, firstName, lastName, email, cancellationToken));
        }

        private async Task<PersonProvisioningResult> ReuseAsync(
            User existing, UserType capacity, CancellationToken cancellationToken)
        {
            // Reuse the person — never a second account for the same phone. If they
            // are appearing in a new capacity (customer ⇄ provider), grant it.
            var granted = existing.EnsureCanActAs(capacity);

            if (granted)
            {
                _logger.LogInformation(
                    "Person {UserId} granted {Capacity} capacity (type is now {Type}) — reusing the account for this phone",
                    existing.Id.Value, capacity, existing.Type);

                await _userRepository.SaveAsync(existing, cancellationToken);
            }

            return new PersonProvisioningResult(existing, IsNewPerson: false, CapacityGranted: granted);
        }

        private async Task<PersonProvisioningResult> CreateUnderPhoneLockAsync(
            PhoneNumber phoneNumber,
            UserType capacity,
            string? firstName,
            string? lastName,
            string? email,
            CancellationToken cancellationToken)
        {
            var database = _dbContext.Database;

            // pg_advisory_xact_lock is released when its transaction ends, so the lock only
            // protects anything if the INSERT commits inside that same transaction. When a
            // caller already has one open, the lock rides along with it and the caller's
            // commit releases it. When nobody does — the OTP handlers save through the
            // UserManagement unit of work with no surrounding transaction, since the pipeline's
            // TransactionBehavior wraps the ServiceCatalog context, not this one — this method
            // owns a transaction of its own and flushes the new person before committing it.
            // The person row is therefore durable before the handler continues; if the
            // handler then fails, an account with no token exists, and the next sign-in reuses
            // it, which is exactly the "one person per phone" outcome this service exists for.
            var ownsTransaction = database.CurrentTransaction is null;
            IDbContextTransaction? transaction = ownsTransaction
                ? await database.BeginTransactionAsync(cancellationToken)
                : null;

            try
            {
                await database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtext({phoneNumber.Value}))",
                    cancellationToken);

                PersonProvisioningResult result;

                var raced = await _userRepository.GetByPhoneNumberAsync(phoneNumber.Value, cancellationToken);
                if (raced is not null)
                {
                    _logger.LogInformation(
                        "Person {UserId} was created concurrently for this phone number — reusing it instead of creating a duplicate",
                        raced.Id.Value);

                    result = await ReuseAsync(raced, capacity, cancellationToken);
                }
                else
                {
                    // Defence in depth: a phone that resolves to nothing above must also be
                    // absent from the uniqueness guard before we create. Protects against a
                    // lookup that missed a legacy row shape.
                    if (await _userRepository.ExistsByPhoneNumberAsync(phoneNumber, cancellationToken))
                    {
                        throw new InvalidOperationException(
                            "A person already exists for this phone number but could not be loaded.");
                    }

                    var person = CreatePerson(phoneNumber, capacity, firstName, lastName, email);
                    await _userRepository.SaveAsync(person, cancellationToken);

                    _logger.LogInformation(
                        "Created person {UserId} with {Capacity} capacity for a previously unknown phone number",
                        person.Id.Value, capacity);

                    result = new PersonProvisioningResult(person, IsNewPerson: true, CapacityGranted: true);
                }

                if (transaction is not null)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }

                return result;
            }
            catch
            {
                if (transaction is not null)
                {
                    try { await transaction.RollbackAsync(cancellationToken); }
                    catch (Exception rollbackFailure)
                    {
                        _logger.LogWarning(rollbackFailure, "Rollback after a failed person provisioning also failed");
                    }
                }

                throw;
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        private static User CreatePerson(
            PhoneNumber phoneNumber,
            UserType capacity,
            string? firstName,
            string? lastName,
            string? email)
        {
            var profile = UserProfile.Create(
                string.IsNullOrWhiteSpace(firstName)
                    ? (capacity == UserType.Customer ? "مشتری" : "ارائه‌دهنده")
                    : firstName,
                string.IsNullOrWhiteSpace(lastName) ? phoneNumber.NationalNumber : lastName,
                middleName: null,
                dateOfBirth: null,
                gender: null);

            // Keep the profile's contact info in step with the account's phone.
            profile.UpdateContactInfo(phoneNumber, null, null);

            // Email remains optional for phone-first accounts; synthesize a stable
            // placeholder only because Email is currently required + unique.
            var resolvedEmail = string.IsNullOrWhiteSpace(email)
                ? Email.Create($"{phoneNumber.NationalNumber}@booksy.{(capacity == UserType.Customer ? "customer" : "provider")}")
                : Email.Create(email);

            return User.RegisterWithPhone(resolvedEmail, phoneNumber, profile, capacity);
        }
    }
}
