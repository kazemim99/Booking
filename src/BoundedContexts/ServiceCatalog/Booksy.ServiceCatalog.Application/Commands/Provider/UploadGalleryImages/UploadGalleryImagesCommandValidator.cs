using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UploadGalleryImages;

public sealed class UploadGalleryImagesCommandValidator : AbstractValidator<UploadGalleryImagesCommand>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public UploadGalleryImagesCommandValidator()
    {
        RuleFor(x => x.ProviderId)
            .NotEmpty()
            .WithMessage("Provider ID is required");

        RuleFor(x => x.Images)
            .NotEmpty()
            .WithMessage("At least one image is required")
            .Must(images => images.Count <= 10)
            .WithMessage("Cannot upload more than 10 images at once");

        RuleForEach(x => x.Images)
            .ChildRules(file =>
            {
                file.RuleFor(f => f.Length)
                    .LessThanOrEqualTo(MaxFileSizeBytes)
                    .WithMessage($"File size must not exceed {MaxFileSizeBytes / 1024 / 1024}MB");

                file.RuleFor(f => f.FileName)
                    .Must(fileName =>
                    {
                        var extension = Path.GetExtension(fileName).ToLowerInvariant();
                        return AllowedExtensions.Contains(extension);
                    })
                    .WithMessage($"Only image files ({string.Join(", ", AllowedExtensions)}) are allowed");
            });
    }
}
