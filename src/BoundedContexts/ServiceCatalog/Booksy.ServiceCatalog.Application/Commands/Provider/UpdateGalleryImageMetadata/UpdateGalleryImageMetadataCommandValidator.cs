using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateGalleryImageMetadata;

public sealed class UpdateGalleryImageMetadataCommandValidator : AbstractValidator<UpdateGalleryImageMetadataCommand>
{
    public UpdateGalleryImageMetadataCommandValidator()
    {
        RuleFor(x => x.ProviderId)
            .NotEmpty()
            .WithMessage("Provider ID is required");

        RuleFor(x => x.ImageId)
            .NotEmpty()
            .WithMessage("Image ID is required");

        When(x => !string.IsNullOrEmpty(x.Caption), () =>
        {
            RuleFor(x => x.Caption)
                .MaximumLength(500)
                .WithMessage("Caption must not exceed 500 characters");
        });

        When(x => !string.IsNullOrEmpty(x.AltText), () =>
        {
            RuleFor(x => x.AltText)
                .MaximumLength(500)
                .WithMessage("Alt text must not exceed 500 characters");
        });
    }
}
