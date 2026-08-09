using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Domain.Services
{
    /// <summary>
    /// The single guarded path for turning a verified phone number into a Person.
    ///
    /// ONE PERSON PER PHONE NUMBER (platform invariant): every flow that could
    /// create an account — customer OTP, provider OTP, invitation acceptance,
    /// email/password registration, seeding — must go through here. The phone is
    /// canonicalized, an existing person is reused and merely granted the requested
    /// capacity, and a new person is created only when the number is genuinely
    /// unknown. Nothing else may call a User factory directly.
    /// </summary>
    public interface IPersonProvisioningService
    {
        Task<PersonProvisioningResult> GetOrCreateByPhoneAsync(
            PhoneNumber phoneNumber,
            UserType capacity,
            string? firstName = null,
            string? lastName = null,
            string? email = null,
            CancellationToken cancellationToken = default);
    }

    /// <param name="Person">The single person owning this phone number.</param>
    /// <param name="IsNewPerson">True when this call created the account.</param>
    /// <param name="CapacityGranted">
    /// True when an existing person gained a new capacity (e.g. a customer who is
    /// now also a provider) — the caller must persist the change.
    /// </param>
    public sealed record PersonProvisioningResult(
        User Person,
        bool IsNewPerson,
        bool CapacityGranted);
}
