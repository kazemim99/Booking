using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateGalleryImageMetadata;

/// <summary>
/// Validates gallery image metadata before it reaches the database.
/// </summary>
/// <remarks>
/// The 500-character limits mirror the persisted column widths (<c>ProviderConfiguration</c> maps both
/// <c>Caption</c> and <c>AltText</c> with <c>HasMaxLength(500)</c>). Without this, an over-long value
/// reached Postgres and surfaced as a <c>DbUpdateException</c> — a 500-class failure for plainly bad
/// input. It only became reachable once the handler started persisting at all.
///
/// <para>This is a FluentValidation validator rather than DataAnnotations on the API request model
/// because the host sets <c>ApiBehaviorOptions.SuppressModelStateInvalidFilter = true</c>, which disables
/// automatic model-state validation application-wide. Attributes on request models are therefore inert;
/// validation runs through the MediatR <c>ValidationBehavior</c> against the command.</para>
/// </remarks>
public sealed class UpdateGalleryImageMetadataCommandValidator
    : AbstractValidator<UpdateGalleryImageMetadataCommand>
{
    private const int MaxTextLength = 500;

    public UpdateGalleryImageMetadataCommandValidator()
    {
        RuleFor(x => x.ProviderId)
            .NotEmpty()
            .WithMessage("Provider ID is required");

        RuleFor(x => x.ImageId)
            .NotEmpty()
            .WithMessage("Image ID is required");

        RuleFor(x => x.Caption)
            .MaximumLength(MaxTextLength)
            .WithMessage($"Caption cannot exceed {MaxTextLength} characters");

        RuleFor(x => x.AltText)
            .MaximumLength(MaxTextLength)
            .WithMessage($"Alt text cannot exceed {MaxTextLength} characters");
    }
}
