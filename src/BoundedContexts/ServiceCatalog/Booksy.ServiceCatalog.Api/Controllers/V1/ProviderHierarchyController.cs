using Booksy.ServiceCatalog.Application.Commands.Membership.AcceptInvitationAsMember;
using Booksy.ServiceCatalog.Application.Commands.Membership.RegisterAndAcceptInvitation;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.ApproveJoinRequest;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.CancelInvitation;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.CancelJoinRequest;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.ConvertToOrganization;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.CreateJoinRequest;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.RejectJoinRequest;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.RemoveStaffMember;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.SendInvitation;
using Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetInvitation;
using Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetPendingInvitations;
using Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetPendingJoinRequests;
using Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetProviderWithStaff;
using Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetSentJoinRequests;
using Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetStaffMembers;
using Booksy.ServiceCatalog.Application.Queries.Membership.GetOrganizationMemberships;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// Manages provider hierarchy operations (organizations, staff members, invitations, join requests)
/// </summary>
/// <remarks>
/// This controller handles all operations related to the provider hierarchy model:
/// - Staff management for organizations
/// - Invitation workflow (send, accept, reject)
/// - Join request workflow (submit, approve, reject)
/// - Provider type conversion (individual to organization)
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/providers/{providerId}/hierarchy")]
[Produces("application/json")]
[Tags("Provider Hierarchy")]
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

    #region Provider Hierarchy Info

    /// <summary>
    /// Get provider with full hierarchy information (parent organization and/or staff members)
    /// </summary>
    /// <remarks>
    /// Returns the provider with its hierarchy context:
    /// - For organizations: includes list of staff members (linked individual providers)
    /// - For individuals: includes parent organization info if linked
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetProviderWithStaffResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProviderWithStaff(Guid providerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProviderWithStaffQuery(providerId), cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Staff Management

    /// <summary>
    /// Get all staff members of an organization
    /// </summary>
    [HttpGet("staff")]
    [ProducesResponseType(typeof(GetStaffMembersResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStaffMembers(Guid providerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetStaffMembersQuery(providerId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get the organization's members from the membership model (roles, status,
    /// provides-services), enriched with each person's name/phone. Supersedes the
    /// legacy <c>staff</c> endpoint above, which reads the old sub-provider model.
    /// </summary>
    [HttpGet("members")]
    [ProducesResponseType(typeof(GetOrganizationMembershipsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMembers(Guid providerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrganizationMembershipsQuery(providerId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Remove a staff member from an organization
    /// </summary>
    [HttpDelete("staff/{staffProviderId}")]
    [ProducesResponseType(typeof(RemoveStaffMemberResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveStaffMember(
        Guid providerId,
        Guid staffProviderId,
        [FromBody] RemoveStaffMemberRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RemoveStaffMemberCommand(providerId, staffProviderId, request.Reason),
            cancellationToken);
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
    /// Send an invitation to join an organization
    /// </summary>
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
    /// COMPATIBILITY SHIM. Delegates to <c>AcceptInvitationAsMemberCommand</c>, the same
    /// command <c>POST /memberships/invitations/{id}/accept</c> uses. Prefer that route.
    ///
    /// This used to send the ProviderHierarchy <c>AcceptInvitationCommand</c>, which called
    /// <c>individualProvider.LinkToOrganization(...)</c> — it required the invitee to already
    /// own an Individual provider and re-parented that provider row under the salon. Working
    /// somewhere does not make a person a Provider, so that handler is gone. The
    /// <c>providerId</c> route segment is now ignored: the invitation identifies the salon,
    /// and the member is the authenticated caller.
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
    /// COMPATIBILITY SHIM. Delegates to <c>RegisterAndAcceptInvitationCommand</c>, the same
    /// command <c>POST /memberships/invitations/{id}/register-and-accept</c> uses. Prefer
    /// that route.
    ///
    /// The previous handler ran a saga that created a brand-new User AND a brand-new
    /// Individual Provider for the invitee — with manual compensating deletes when a step
    /// failed — then cloned the organization's services, working hours and gallery onto that
    /// shadow provider. An employee is a membership of the salon, so there is no second
    /// provider to create and nothing to clone; that handler is gone.
    ///
    /// Behaviour changes callers should know about: the OTP is verified against the phone
    /// the invitation was ISSUED to (a phone in the body is ignored, so an invitation cannot
    /// be redirected to a different number), an existing account on that phone is REUSED
    /// rather than duplicated, the clone flags are ignored, and no session tokens are
    /// returned — the new member signs in with their own phone through the normal OTP flow.
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

    #region Join Requests

    /// <summary>
    /// Get pending join requests for an organization
    /// </summary>
    [HttpGet("join-requests")]
    [ProducesResponseType(typeof(GetPendingJoinRequestsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPendingJoinRequests(Guid providerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPendingJoinRequestsQuery(providerId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a join request to join an organization (called by an individual provider)
    /// </summary>
    [HttpPost("join-requests")]
    [ProducesResponseType(typeof(CreateJoinRequestResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateJoinRequest(
        Guid providerId,
        [FromBody] CreateJoinRequestRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateJoinRequestCommand(providerId, request.RequesterId, request.Message),
            cancellationToken);
        return CreatedAtAction(nameof(GetPendingJoinRequests), new { providerId }, result);
    }

    /// <summary>
    /// Approve a join request
    /// </summary>
    [HttpPost("join-requests/{requestId}/approve")]
    [ProducesResponseType(typeof(ApproveJoinRequestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveJoinRequest(
        Guid providerId,
        Guid requestId,
        [FromBody] ApproveJoinRequestRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ApproveJoinRequestCommand(requestId, request.ReviewerId, request.Note),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Reject a join request
    /// </summary>
    [HttpPost("join-requests/{requestId}/reject")]
    [ProducesResponseType(typeof(RejectJoinRequestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectJoinRequest(
        Guid providerId,
        Guid requestId,
        [FromBody] RejectJoinRequestRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RejectJoinRequestCommand(requestId, request.ReviewerId, request.Reason),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get join requests sent by an individual provider
    /// </summary>
    [HttpGet("join-requests/sent")]
    [ProducesResponseType(typeof(GetSentJoinRequestsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSentJoinRequests(Guid providerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSentJoinRequestsQuery(providerId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cancel a pending join request (withdraw by requester)
    /// </summary>
    [HttpDelete("join-requests/{requestId}")]
    [ProducesResponseType(typeof(CancelJoinRequestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelJoinRequest(
        Guid providerId,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CancelJoinRequestCommand(requestId, providerId),
            cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Conversion

    /// <summary>
    /// Convert an individual provider to an organization
    /// </summary>
    [HttpPost("convert-to-organization")]
    [ProducesResponseType(typeof(ConvertToOrganizationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConvertToOrganization(Guid providerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ConvertToOrganizationCommand(providerId), cancellationToken);
        return Ok(result);
    }

    #endregion
}

#region Request DTOs

public record SendInvitationRequest(
    string InviteePhoneNumber,
    string? InviteeName = null,
    string? FirstName = null,
    string? LastName = null,
    string? Email = null,
    string? Message = null);

public record AcceptInvitationWithRegistrationRequest(
    string PhoneNumber,
    string FirstName,
    string LastName,
    string? Email,
    string OtpCode,
    bool CloneServices,
    bool CloneWorkingHours,
    bool CloneGallery);

public record RemoveStaffMemberRequest(string Reason);

public record CreateJoinRequestRequest(
    Guid RequesterId,
    string? Message = null);

public record ApproveJoinRequestRequest(
    Guid ReviewerId,
    string? Note = null);

public record RejectJoinRequestRequest(
    Guid ReviewerId,
    string? Reason = null);

#endregion
