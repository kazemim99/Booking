// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/UpdateBusinessProfile/UpdateBusinessProfileCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessProfile
{
    public sealed class UpdateBusinessProfileCommandValidator : AbstractValidator<UpdateBusinessProfileCommand>
    {
        public UpdateBusinessProfileCommandValidator()
        {
            RuleFor(x => x.ProviderId)
                .NotEmpty()
                .WithMessage("Provider ID is required");

            RuleFor(x => x.BusinessName)
                .NotEmpty()
                .WithMessage("Business name is required")
                .MaximumLength(200)
                .WithMessage("Business name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required")
                .MaximumLength(1000)
                .WithMessage("Description cannot exceed 1000 characters");

            When(x => !string.IsNullOrEmpty(x.Website), () =>
            {
                RuleFor(x => x.Website)
                    .Must(BeValidUrl)
                    .WithMessage("Website must be a valid URL");
            });

            When(x => x.Tags != null, () =>
            {
                RuleFor(x => x.Tags)
                    .Must(tags => tags!.Count <= 20)
                    .WithMessage("Maximum 20 tags allowed");
            });
        }

        private static bool BeValidUrl(string? url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }
}