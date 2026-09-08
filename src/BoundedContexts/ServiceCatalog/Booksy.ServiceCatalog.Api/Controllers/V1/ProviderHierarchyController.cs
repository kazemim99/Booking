using Booksy.ServiceCatalog.Application.Commands.Membership.AcceptInvitationAsMember;
using Booksy.ServiceCatalog.Application.Commands.Membership.RegisterAndAcceptInvitation;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.CancelInvitation;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.SendInvitation;
using Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetInvitation;
using Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetPendingInvitations;
using Booksy.ServiceCatalog.Application.Queries.Membership.GetOrganizationMemberships;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// A salon's roster and the invitations that build it.
/// </summary>
/// <remarks>
/// The route keeps the <c>/hierarchy</c> segment for now, but there is no hierarchy left:
/// a salon is a Provider, and everyone who works there is an
/// <c>OrganizationMembership</c> of it. Removed with the provider-hierarchy model:
/// <list type="bullet">
///   <item>join requests (a Provider asking to join another Provider),</item>
///   <item>convert-to-organization,</item>
///   <item>the <c>staff</c> read and delete, which operated on sub-provider rows.</item>
/// </list>
/// Terminating a membership lives on <c>MembershipsController</c>
/// (<c>POST /memberships/{id}/terminate</c>), because removal is a membership lifecycle
/// transition rather than an edit to the salon.
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/providers/{providerId}/hierarchy")]
[Produces("application/json")]
[Tags("Provider Members")]
[Authorize]
public class ProviderHierarchyController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<ProviderHierarchyController> _logger;

    public ProviderHierarchyController(ISender mediator, ILogger<ProviderHierarchyController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Members

    /// <summary>
    /// The organization's members (roles, status, provides-services), enriched with each
    /// person's name and phone. Unclaimed members — people the salon manages who have no
    /// app account — appear with their display name and <c>isUnclaimed</c> set.
    /// </summary>
    [HttpGet("members")]
    [ProducesResponseType(typeof(GetOrganizationMembershipsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMembers(Guid providerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrganizationMembershipsQuery(providerId), cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Invitations

    /// <summary>
    /// Get pending invitations sent by an organization
    /// </summary>
    [HttpGet("invitations")]
    [ProducesResponseType(typeof(GetPendingInvitationsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPendingInvitations(Guid providerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPendingInvitationsQuery(providerId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific invitation by ID (public endpoint for invitation acceptance page)
    /// </summary>
    [HttpGet("invitations/{invitationId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetInvitationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvitation(
        Guid providerId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInvitationQuery(providerId, invitationId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Invite someone to join the salon, by phone number.
    /// </summary>
    /// <remarks>
    /// The phone identifies the person: if an account already exists on it, accepting
    /// reuses that account rather than creating a second one.
    /// </remarks>
    [HttpPost("invitations")]
    [ProducesResponseType(typeof(SendInvitationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendInvitation(
        Guid providerId,
        [FromBody] SendInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SendInvitationCommand(providerId, request.InviteePhoneNumber, request.InviteeName, request.Message),
            cancellationToken);
        return CreatedAtAction(nameof(GetPendingInvitations), new { providerId }, result);
    }

    /// <summary>
    /// Accept an invitation (called by the invited person).
    /// </summary>
    /// <remarks>
    /// Delegates to the same command as <c>POST /memberships/invitations/{id}/accept</c>;
    /// prefer that route. The <c>providerId</c> segment is ignored — the invitation
    /// identifies the salon, and the member is the authenticated caller. Accepting creates
    /// a membership; it does not make the person a Provider.
    /// </remarks>
    [HttpPost("invitations/{invitationId}/accept")]
    [ProducesResponseType(typeof(AcceptInvitationAsMemberResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptInvitation(
        Guid providerId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AcceptInvitationAsMemberCommand(invitationId),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Accept an invitation as someone who does not have an account yet.
    /// </summary>
    /// <remarks>
    /// Delegates to <c>POST /memberships/invitations/{id}/register-and-accept</c>; prefer
    /// that route. The OTP is verified against the phone the invitation was ISSUED to, so a
    /// phone in the body is ignored and an invitation cannot be redirected elsewhere. An
    /// existing account on that phone is reused rather than duplicated, and no session
    /// tokens are returned — the new member signs in through the normal OTP flow.
    /// </remarks>
    [HttpPost("invitations/{invitationId}/accept-with-registration")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterAndAcceptInvitationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptInvitationWithRegistration(
        Guid providerId,
        Guid invitationId,
        [FromBody] AcceptInvitationWithRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RegisterAndAcceptInvitationCommand(
                InvitationId: invitationId,
                FirstName: request.FirstName,
                LastName: request.LastName,
                Email: request.Email,
                OtpCode: request.OtpCode),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Cancel a pending invitation (called by the organization that sent it)
    /// </summary>
    [HttpDelete("invitations/{invitationId}")]
    [ProducesResponseType(typeof(CancelInvitationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelInvitation(
        Guid providerId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CancelInvitationCommand(invitationId, providerId),
            cancellationToken);
        return Ok(result);
    }

    #endregion
}

/// <summary>Request body for sending an invitation.</summary>
public sealed record SendInvitationRequest(
    string InviteePhoneNumber,
    string? InviteeName,
    string? Message);

/// <summary>
/// Request body for accepting an invitation without an existing account. PhoneNumber and
/// the clone flags the old provider-hierarchy contract carried are gone: the phone comes
/// from the invitation, and a member works from the salon's own services and hours, so
/// there is nothing to clone.
/// </summary>
public sealed record AcceptInvitationWithRegistrationRequest(
    string FirstName,
    string LastName,
    string? Email,
    string OtpCode);
