using Booksy.ServiceCatalog.Application.Commands.Provider.Registration;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetRegistrationProgress;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Booksy.ServiceCatalog.Api.Controllers.V1;

/// <summary>
/// Progressive provider registration endpoints
/// Handles step-by-step registration flow with separate endpoints for each step
///
/// NOTE: User must be authenticated before accessing these endpoints.
/// Authentication happens after phone verification via /api/v1/phone-verification/register endpoint.
/// </summary>
[ApiController]
[Route("api/v1/registration")]
[Authorize]
public class ProviderRegistrationController : ControllerBase
{
    private readonly ISender _sender;

    public ProviderRegistrationController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get current registration progress and draft data
    /// Used to resume registration flow
    /// </summary>
    /// <response code="200">Returns registration progress</response>
    /// <response code="404">No draft found</response>
    [HttpGet("progress")]
    [ProducesResponseType(typeof(GetRegistrationProgressResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProgress(CancellationToken cancellationToken)
    {
        var query = new GetRegistrationProgressQuery();
        var result = await _sender.Send(query, cancellationToken);

        return result.HasDraft ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Step 3: Save location information and create provider draft
    /// This is the first step that persists data to the database
    /// </summary>
    /// <param name="command">Location and business information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="201">Provider draft created</response>
    /// <response code="200">Existing draft updated</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost("step-3/location")]
    [ProducesResponseType(typeof(SaveStep3LocationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SaveStep3LocationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveStep3Location(
        [FromBody] SaveStep3LocationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        // Return 201 if message contains "created", otherwise 200
        return result.Message.Contains("created", StringComparison.OrdinalIgnoreCase)
            ? Created($"/api/v1/providers/{result.ProviderId}", result)
            : Ok(result);
    }

    /// <summary>
    /// Step 4: Save services to provider draft
    /// Requires Step 3 to be completed (provider draft must exist)
    /// </summary>
    /// <param name="command">Services data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Services saved successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Provider draft not found</response>
    [HttpPost("step-4/services")]
    [ProducesResponseType(typeof(SaveStep4ServicesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveStep4Services(
        [FromBody] SaveStep4ServicesCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Step 5: Save staff/team members to provider draft
    /// Requires Step 3 to be completed (provider draft must exist)
    /// </summary>
    /// <param name="command">Staff members data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Staff members saved successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Provider draft not found</response>
    [HttpPost("step-5/staff")]
    [ProducesResponseType(typeof(SaveStep5StaffResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveStep5Staff(
        [FromBody] SaveStep5StaffCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Step 6: Save working hours to provider draft
    /// Requires Step 3 to be completed (provider draft must exist)
    /// </summary>
    /// <param name="command">Business hours data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Business hours saved successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Provider draft not found</response>
    [HttpPost("step-6/working-hours")]
    [ProducesResponseType(typeof(SaveStep6WorkingHoursResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveStep6WorkingHours(
        [FromBody] SaveStep6WorkingHoursCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Step 7: Upload gallery images to provider draft
    /// This endpoint uploads images and marks the gallery step as complete
    /// </summary>
    /// <param name="files">Image files to upload (max 10 files, 10MB each)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Gallery images uploaded successfully</response>
    /// <response code="400">Invalid files or validation errors</response>
    /// <response code="404">Provider draft not found</response>
    [HttpPost("step-7/gallery")]
    [RequestSizeLimit(52428800)] // 50MB for multiple files
    [ProducesResponseType(typeof(SaveStep7GalleryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveStep7Gallery(
        [FromForm] IFormFileCollection files,
        CancellationToken cancellationToken)
    {
        // Get current user's provider draft
        var progressQuery = new GetRegistrationProgressQuery();
        var progress = await _sender.Send(progressQuery, cancellationToken);

        if (!progress.HasDraft || !progress.ProviderId.HasValue)
        {
            return NotFound(new { message = "Provider draft not found" });
        }

        // Upload gallery images using the shared handler
        var uploadCommand = new Application.Commands.Provider.UploadGalleryImages.UploadGalleryImagesCommand(
            progress.ProviderId.Value,
            files);

        var uploadedImages = await _sender.Send(uploadCommand, cancellationToken);

        // Return response in Step 7 format for consistency with other registration steps
        return Ok(new SaveStep7GalleryResult(
            progress.ProviderId.Value,
            7, // Registration step
            uploadedImages.Count,
            $"Gallery step completed. {uploadedImages.Count} image(s) uploaded."));
    }

    /// <summary>
    /// Step 8: Save optional feedback
    /// This is an optional step - providers can skip providing feedback
    /// </summary>
    /// <param name="command">Feedback data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Feedback saved successfully</response>
    /// <response code="404">Provider draft not found</response>
    [HttpPost("step-8/feedback")]
    [ProducesResponseType(typeof(SaveStep8FeedbackResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveStep8Feedback(
        [FromBody] SaveStep8FeedbackCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Step 9: Complete provider registration
    /// Validates all required data and transitions provider to PendingVerification status
    /// </summary>
    /// <param name="command">Completion data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Registration completed successfully</response>
    /// <response code="400">Validation failed - missing required data</response>
    /// <response code="404">Provider draft not found</response>
    [HttpPost("step-9/complete")]
    [ProducesResponseType(typeof(SaveStep9CompleteResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveStep9Complete(
        [FromBody] SaveStep9CompleteCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
