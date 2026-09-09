namespace Booksy.ServiceCatalog.Application.Services.Interfaces;

/// <summary>
/// The two UserManagement account operations the invitation register-and-accept flow needs:
/// create an account for a phone that resolved to nobody, and compensate by removing it if a
/// later step in that same flow fails. Kept narrow and separate from
/// <see cref="IInvitationRegistrationService"/>'s other members (OTP, individual-provider
/// creation, token generation) precisely because these two are the ones that cross into
/// UserManagement's identity store — see the implementing type in <c>Booksy.Host.Composition</c>
/// for why that seam belongs in the Host.
/// </summary>
/// <summary>
/// What <see cref="IPersonAccountProvisioningService.CreateWithPhoneAsync"/> did: the person's
/// id, and whether this call created the account or reused one that already existed (because
/// the phone was registered between the caller's own lookup and this call). The flag is what a
/// caller's compensation must key on — an account you did not create is not yours to delete.
/// </summary>
public sealed record PersonAccountCreation(Guid PersonId, bool IsNewAccount);

public interface IPersonAccountProvisioningService
{
    /// <summary>
    /// Creates (or, if the phone resolved to someone in the moment between the caller's own
    /// check and this call, reuses) an account for the given phone number. Returns the
    /// person's id and whether the account was created by this call.
    /// </summary>
    Task<PersonAccountCreation> CreateWithPhoneAsync(
        string phoneNumber,
        string? firstName,
        string? lastName,
        string? email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compensation: soft-deletes an account (matching the platform's existing Deleted-status
    /// convention, which every phone/email uniqueness check already excludes) so a failed
    /// registration does not permanently claim the phone number. Never throws for a missing
    /// user or a persistence failure — reports the outcome instead, the same "best effort"
    /// contract the loopback it replaces had.
    /// </summary>
    Task<bool> DeleteAsync(
        Guid personId,
        string reason,
        CancellationToken cancellationToken = default);
}
