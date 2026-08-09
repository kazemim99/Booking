using Booksy.ServiceCatalog.Api.Models.Requests;
using Booksy.ServiceCatalog.Application.Commands.Membership.AcceptInvitationAsMember;
using Booksy.ServiceCatalog.Application.Commands.Membership.ChangeMembershipRoles;
using Booksy.ServiceCatalog.Application.Commands.Membership.RegisterAndAcceptInvitation;
using Booksy.ServiceCatalog.Application.Commands.Membership.RevokeInvitation;
using Booksy.ServiceCatalog.Application.Commands.Membership.TerminateMembership;
using Booksy.ServiceCatalog.Application.Queries.Membership.GetInvitationSummary;
using Booksy.ServiceCatalog.Application.Queries.Membership.GetMyMemberships;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Booksy.ServiceCatalog.Api.Controllers.V1;

/// <summary>
/// Person-scoped organization-membership endpoints. Unlike the hierarchy controller,
/// these are keyed on the authenticated person (not a providerId), because a person can
/// belong to several organizations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/memberships")]
[Produces("application/json")]
[Tags("Memberships")]
[Authorize]
public class MembershipsController : ControllerBase
{
    private readonly ISender _mediator;

    public MembershipsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// The authenticated person's organization memberships (for the salon switcher /
    /// multi-membership session). Excludes terminated memberships.
    /// </summary>
    /// <response code="200">The caller's memberships</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(GetMyMembershipsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyMemberships(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyMembershipsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Public summary of an invitation for the accept screen opened from an SMS link
    /// (the opener has no account yet). The phone is masked.
    /// </summary>
    /// <response code="200">Invitation summary (null body if not found)</response>
    [HttpGet("invitations/{invitationId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InvitationSummaryResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvitationSummary(
        Guid invitationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInvitationSummaryQuery(invitationId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Accept an invitation as the authenticated (already-registered) person, becoming an
    /// active member of the inviting organization. The person's account is reused; a new
    /// membership is created only if they are not already a member.
    /// </summary>
    /// <response code="200">Membership activated</response>
    /// <response code="400">Invitation not pending, expired, or sent to a different phone</response>
    /// <response code="404">Invitation not found</response>
    [HttpPost("invitations/{invitationId:guid}/accept")]
    [ProducesResponseType(typeof(AcceptInvitationAsMemberResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptInvitation(Guid invitationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AcceptInvitationAsMemberCommand(invitationId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// New-user path: register (name) + verify the OTP sent to the invited phone +
    /// accept, in one call. Anonymous — the caller has no account yet. The account is
    /// reused if the phone already exists, so it never creates a duplicate person.
    /// </summary>
    /// <response code="200">Account created/reused and membership activated</response>
    /// <response code="400">Invalid/expired OTP, or invitation not pending</response>
    /// <response code="404">Invitation not found</response>
    [HttpPost("invitations/{invitationId:guid}/register-and-accept")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterAndAcceptInvitationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterAndAccept(
        Guid invitationId,
        [FromBody] RegisterAndAcceptInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RegisterAndAcceptInvitationCommand(
                invitationId, request.FirstName, request.LastName, request.Email, request.OtpCode),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Terminate a membership (S5): remove a staff member (as owner) or leave an
    /// organization (as the member). An organization can never lose its last owner.
    /// </summary>
    /// <response code="200">Membership terminated</response>
    /// <response code="400">Would remove the last owner</response>
    /// <response code="403">Caller may not terminate this membership</response>
    /// <response code="404">Membership not found</response>
    [HttpPost("{membershipId:guid}/terminate")]
    [ProducesResponseType(typeof(TerminateMembershipResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Terminate(
        Guid membershipId,
        [FromBody] TerminateMembershipRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new TerminateMembershipCommand(membershipId, request.Reason),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Withdraw a pending invitation (owner-only). The invitation is closed, not
    /// deleted — the attempt remains in the membership audit trail.
    /// </summary>
    /// <response code="200">Invitation revoked</response>
    /// <response code="400">Invitation is not pending</response>
    /// <response code="403">Caller is not an owner of the inviting organization</response>
    /// <response code="404">Invitation not found</response>
    [HttpPost("invitations/{invitationId:guid}/revoke")]
    [ProducesResponseType(typeof(RevokeInvitationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeInvitation(
        Guid invitationId,
        [FromBody] RevokeInvitationRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RevokeInvitationCommand(invitationId, request?.Reason), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Replace a membership's organization-scoped roles (owner-only). The organization
    /// can never be left without an owner.
    /// </summary>
    /// <response code="200">Roles updated</response>
    /// <response code="400">Empty/unknown role, or would remove the last owner</response>
    /// <response code="403">Caller is not an owner</response>
    /// <response code="404">Membership not found</response>
    [HttpPatch("{membershipId:guid}/roles")]
    [ProducesResponseType(typeof(ChangeMembershipRolesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRoles(
        Guid membershipId,
        [FromBody] ChangeMembershipRolesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ChangeMembershipRolesCommand(membershipId, request.Roles),
            cancellationToken);
        return Ok(result);
    }
}
