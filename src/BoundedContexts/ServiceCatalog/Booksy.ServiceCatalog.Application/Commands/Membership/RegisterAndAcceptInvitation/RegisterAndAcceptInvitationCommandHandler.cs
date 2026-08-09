using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Identity;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates.MembershipAuditAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Membership.RegisterAndAcceptInvitation;

public sealed class RegisterAndAcceptInvitationCommandHandler
    : ICommandHandler<RegisterAndAcceptInvitationCommand, RegisterAndAcceptInvitationResult>
{
    private readonly IProviderInvitationReadRepository _invitationReadRepository;
    private readonly IProviderInvitationWriteRepository _invitationWriteRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IMembershipAuditRepository _auditRepository;
    private readonly IPersonDirectory _personDirectory;
    private readonly IInvitationRegistrationService _registrationService;
    private readonly IServiceCatalogUnitOfWork _unitOfWork;
    private readonly IMemberBookabilityService _memberBookability;
    private readonly ILogger<RegisterAndAcceptInvitationCommandHandler> _logger;

    public RegisterAndAcceptInvitationCommandHandler(
        IProviderInvitationReadRepository invitationReadRepository,
        IProviderInvitationWriteRepository invitationWriteRepository,
        IOrganizationMembershipRepository membershipRepository,
        IMembershipAuditRepository auditRepository,
        IPersonDirectory personDirectory,
        IInvitationRegistrationService registrationService,
        IServiceCatalogUnitOfWork unitOfWork,
        IMemberBookabilityService memberBookability,
        ILogger<RegisterAndAcceptInvitationCommandHandler> logger)
    {
        _invitationReadRepository = invitationReadRepository;
        _invitationWriteRepository = invitationWriteRepository;
        _membershipRepository = membershipRepository;
        _auditRepository = auditRepository;
        _personDirectory = personDirectory;
        _registrationService = registrationService;
        _unitOfWork = unitOfWork;
        _memberBookability = memberBookability;
        _logger = logger;
    }

    public async Task<RegisterAndAcceptInvitationResult> Handle(
        RegisterAndAcceptInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var invitation = await _invitationReadRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new NotFoundException($"Invitation with ID {request.InvitationId} not found");

        if (invitation.Status != InvitationStatus.Pending)
            throw new DomainValidationException($"Invitation is no longer pending (status: {invitation.Status})");

        var phone = invitation.PhoneNumber.Value;

        // The OTP was sent to the invited phone, so verifying it proves the caller
        // owns exactly that number.
        var otpValid = await _registrationService.VerifyOtpAsync(phone, request.OtpCode, cancellationToken);
        if (!otpValid)
            throw new DomainValidationException("Invalid or expired OTP code");

        // Reuse an existing account by phone; only create one when the phone is truly
        // unknown. Either way there is exactly one person per phone.
        var existing = await _personDirectory.FindByPhoneAsync(phone, cancellationToken);
        UserId personId;
        bool isNewAccount;
        if (existing is not null)
        {
            personId = UserId.From(existing.PersonId);
            isNewAccount = false;
        }
        else
        {
            personId = await _registrationService.CreateUserWithPhoneAsync(
                phone, request.FirstName, request.LastName, request.Email, cancellationToken);
            isNewAccount = true;
        }

        try
        {
            var membership = await _membershipRepository.GetActiveByPersonAndOrganizationAsync(
                personId, invitation.OrganizationId, cancellationToken);

            if (membership is null)
            {
                membership = OrganizationMembership.InviteExisting(personId, invitation.OrganizationId);
                membership.Accept();
                await _membershipRepository.SaveAsync(membership, cancellationToken);
            }

            invitation.AcceptByMember();
            await _invitationWriteRepository.UpdateAsync(invitation, cancellationToken);

            await _auditRepository.AppendAsync(
                MembershipAuditEntry.Record(
                    membership.Id,
                    membership.OrganizationId,
                    MembershipAuditAction.Accepted,
                    membership.Status,
                    subjectPersonId: membership.PersonId,
                    actorPersonId: personId,
                    roles: membership.Roles,
                    invitationId: invitation.Id),
                cancellationToken);

            // Newly registered member is bookable immediately.
            await _memberBookability.SyncAsync(membership, cancellationToken);

            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation(
                "Register-and-accept: invitation {InvitationId} → person {PersonId} (new={IsNew}) membership {MembershipId} in org {OrgId}",
                invitation.Id, personId.Value, isNewAccount, membership.Id, invitation.OrganizationId.Value);

            return new RegisterAndAcceptInvitationResult(
                PersonId: personId.Value,
                MembershipId: membership.Id,
                OrganizationId: invitation.OrganizationId.Value,
                IsNewAccount: isNewAccount,
                Roles: membership.Roles.Select(r => r.ToString()).ToList(),
                AcceptedAt: invitation.RespondedAt!.Value);
        }
        catch (Exception ex) when (isNewAccount)
        {
            // Compensation: don't leave an orphaned account behind if the membership
            // step fails (the account we just created has nothing attached to it).
            _logger.LogError(ex,
                "Register-and-accept failed after creating account {PersonId}; compensating by deleting it",
                personId.Value);
            await _registrationService.DeleteUserAsync(
                personId, $"register-and-accept failed: {ex.Message}", cancellationToken);
            throw;
        }
    }
}
