using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.UserManagement.Application.Abstractions.Persistence;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.Services;

namespace Booksy.Host.Composition;

/// <summary>
/// In-process implementation of ServiceCatalog's <see cref="IPersonAccountProvisioningService"/>
/// port: the two UserManagement account operations the invitation register-and-accept flow
/// needs (create, and compensating delete), implemented by dispatching directly into
/// UserManagement's own domain services instead of a loopback HTTP call to itself.
///
/// <para><b>Why this lives in the Host.</b> Same reason as
/// <see cref="InProcessProviderInfoService"/> and <see cref="InProcessTokenService"/>: this is
/// the composition root, the one project that references both bounded contexts, so
/// ServiceCatalog stays free of a compile-time dependency on UserManagement's identity types.</para>
///
/// <para><b>What it replaces, and why it was worse than just "a network hop."</b>
/// <see cref="Booksy.ServiceCatalog.Infrastructure.Services.Application.InvitationRegistrationService"/>
/// used to POST/DELETE <c>/api/v1/users</c> over an <c>HttpClient</c>:</para>
/// <list type="bullet">
///   <item><description><b>Create</b> reached a real, working endpoint, so it "worked" — but
///   only by re-deriving, over HTTP, exactly what
///   <see cref="IPersonProvisioningService.GetOrCreateByPhoneAsync"/> already does in-process
///   (the canonical "one person per phone" guarded path this platform's own domain-service doc
///   comment says every account-creating flow, invitation acceptance included, must use).</description></item>
///   <item><description><b>Delete</b> (the failure-compensation path) could never have worked at
///   all: the target endpoint requires the <c>SysAdminOnly</c> policy, which this
///   unauthenticated flow's caller never carries, and — more fundamentally —
///   <c>DeleteUserCommand</c> has no MediatR handler registered anywhere in the codebase, so the
///   request would 500 even with the right role. Both outcomes were swallowed by the caller's
///   own try/catch and logged as a warning, so every register-and-accept that created an
///   account and then failed a later step has been silently leaving that account behind —
///   permanently claiming its phone number, since only a Deleted-status row is excluded from
///   the uniqueness checks <see cref="IPersonProvisioningService"/> relies on.</description></item>
/// </list>
/// </summary>
/// <remarks>
/// Public for the same reason as the other in-process adapters: so
/// <c>Booksy.Host.CompositionTests</c> can assert the container resolves this exact type.
/// </remarks>
public sealed class InProcessPersonAccountProvisioningService : IPersonAccountProvisioningService
{
    private readonly IPersonProvisioningService _provisioning;
    private readonly IUserRepository _userRepository;
    private readonly IUserManagementUnitOfWork _unitOfWork;
    private readonly ILogger<InProcessPersonAccountProvisioningService> _logger;

    public InProcessPersonAccountProvisioningService(
        IPersonProvisioningService provisioning,
        IUserRepository userRepository,
        IUserManagementUnitOfWork unitOfWork,
        ILogger<InProcessPersonAccountProvisioningService> logger)
    {
        _provisioning = provisioning;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> CreateWithPhoneAsync(
        string phoneNumber,
        string? firstName,
        string? lastName,
        string? email,
        CancellationToken cancellationToken = default)
    {
        var phone = PhoneNumber.From(phoneNumber);
        var result = await _provisioning.GetOrCreateByPhoneAsync(
            phone, UserType.Provider, firstName, lastName, email, cancellationToken);

        // PersonProvisioningService only tracks the new/updated user (SaveAsync); the caller
        // (a ServiceCatalog command handler) has its own TransactionBehavior commit its own
        // IServiceCatalogUnitOfWork, which never touches this UserManagementDbContext. Without
        // committing here explicitly, the person would exist only in the change tracker and
        // vanish at the end of the request -- the same class of gap already found and fixed
        // in CompleteProviderAuthenticationCommandHandler for the same reason.
        if (result.IsNewPerson || result.CapacityGranted)
            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

        return result.Person.Id.Value;
    }

    public async Task<bool> DeleteAsync(
        Guid personId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = UserId.From(personId);
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                _logger.LogWarning(
                    "Compensation delete requested for {UserId}, but no such user exists", personId);
                return false;
            }

            // Soft-delete, matching the platform's existing convention: every phone/email
            // uniqueness check already excludes Deleted-status rows, so this frees the phone
            // number for a genuine future registration rather than leaving it permanently
            // claimed by an account that never got past this failed sign-up.
            user.SetStatus(UserStatus.Deleted);
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation(
                "Compensation: soft-deleted user {UserId} ({Reason})", personId, reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Compensation delete failed for user {UserId}", personId);
            return false;
        }
    }
}
