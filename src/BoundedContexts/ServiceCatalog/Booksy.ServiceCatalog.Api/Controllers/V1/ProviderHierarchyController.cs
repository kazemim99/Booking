using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.AcceptInvitation;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.ApproveJoinRequest;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.ConvertToOrganization;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.CreateJoinRequest;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.RejectJoinRequest;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.RemoveStaffMember;
using Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.SendInvitation;
using Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetPendingInvitations;
using Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetPendingJoinRequests;
using Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetProviderWithStaff;
using Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetStaffMembers;
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
            new SendInvitationCommand(providerId, request.PhoneNumber, request.InviteeName, request.Message),
            cancellationToken);
        return CreatedAtAction(nameof(GetPendingInvitations), new { providerId }, result);
    }

    /// <summary>
    /// Accept an invitation (called by the invited provider)
    /// </summary>
    [HttpPost("invitations/{invitationId}/accept")]
    [ProducesResponseType(typeof(AcceptInvitationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptInvitation(
        Guid providerId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AcceptInvitationCommand(invitationId, providerId),
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
    string PhoneNumber,
    string? InviteeName = null,
    string? Message = null);

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
