using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.Services;
using Booksy.UserManagement.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Infrastructure.Services.Domain
{
    /// <inheritdoc cref="IPersonProvisioningService"/>
    public sealed class PersonProvisioningService : IPersonProvisioningService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<PersonProvisioningService> _logger;

        public PersonProvisioningService(
            IUserRepository userRepository,
            ILogger<PersonProvisioningService> logger)
        {
            _userRepository = userRepository;
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

            return new PersonProvisioningResult(person, IsNewPerson: true, CapacityGranted: true);
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
