using System.Security.Claims;
using AsanRezerve.ServiceCatalog.Application.Customers;
using AsanRezerve.ServiceCatalog.Application.Queries.Membership.CanManageOrganization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsanRezerve.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// A salon's own customer book: customers the provider adds by hand or imports from the contacts they
/// tick on their phone, so booking a regular is a tap. Only the salon's members see or change it.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/providers/{providerId:guid}/customers")]
[Produces("application/json")]
public class ProviderCustomersController : ControllerBase
{
    private readonly ISender _mediator;

    public ProviderCustomersController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>The salon's customers by name, optionally filtered by a name or phone fragment.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProviderCustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid providerId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        if (!await CanManageProvider(providerId))
            return Forbid();

        return Ok(await _mediator.Send(new GetProviderCustomersQuery(providerId, search), cancellationToken));
    }

    /// <summary>Adds one customer. A number the salon already has is refused with 409.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProviderCustomerDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Add(Guid providerId, [FromBody] ProviderCustomerRequest request, CancellationToken cancellationToken)
    {
        if (!await CanManageProvider(providerId))
            return Forbid();

        var customer = await _mediator.Send(new AddProviderCustomerCommand(
            providerId, request.FirstName ?? string.Empty, request.LastName, request.PhoneNumber ?? string.Empty, request.Notes),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, customer);
    }

    [HttpPut("{customerId:guid}")]
    [ProducesResponseType(typeof(ProviderCustomerDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid providerId, Guid customerId, [FromBody] ProviderCustomerRequest request, CancellationToken cancellationToken)
    {
        if (!await CanManageProvider(providerId))
            return Forbid();

        return Ok(await _mediator.Send(new UpdateProviderCustomerCommand(
            providerId, customerId, request.FirstName ?? string.Empty, request.LastName, request.PhoneNumber ?? string.Empty, request.Notes),
            cancellationToken));
    }

    /// <summary>Removes the customer from the book. Past bookings are untouched.</summary>
    [HttpDelete("{customerId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(Guid providerId, Guid customerId, CancellationToken cancellationToken)
    {
        if (!await CanManageProvider(providerId))
            return Forbid();

        await _mediator.Send(new RemoveProviderCustomerCommand(providerId, customerId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Imports the contacts the provider ticked in the phone's picker. Per-contact outcome; numbers the
    /// salon already has are left exactly as saved; invalid numbers are reported and the rest still import.
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ImportProviderCustomersResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Import(Guid providerId, [FromBody] ImportProviderCustomersRequest request, CancellationToken cancellationToken)
    {
        if (!await CanManageProvider(providerId))
            return Forbid();

        return Ok(await _mediator.Send(
            new ImportProviderCustomersCommand(providerId, request.Customers ?? new List<ImportCustomerRow>()),
            cancellationToken));
    }

    private async Task<bool> CanManageProvider(Guid providerId)
    {
        if (string.IsNullOrEmpty(User.FindFirstValue(ClaimTypes.NameIdentifier)))
            return false;

        if (User.IsInRole("Admin") || User.IsInRole("Administrator") || User.IsInRole("SysAdmin"))
            return true;

        if (User.FindFirst("providerId")?.Value == providerId.ToString())
            return true;

        // Any active member who books for the salon may use its customer book.
        return await _mediator.Send(new CanManageOrganizationQuery(providerId, OrganizationPermission.ManageBookings));
    }
}

public sealed record ProviderCustomerRequest(string? FirstName, string? LastName, string? PhoneNumber, string? Notes);

public sealed record ImportProviderCustomersRequest(List<ImportCustomerRow>? Customers);
