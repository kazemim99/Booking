// ========================================
// Booksy.ServiceCatalog.Application/Commands/Service/UpdateService/UpdateServiceCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Service.UpdateService
{
    public sealed class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
    {
        public UpdateServiceCommandValidator()
        {
            RuleFor(x => x.ServiceId)
                .NotEmpty()
                .WithMessage("Service ID is required");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Service name is required")
                .MaximumLength(200)
                .WithMessage("Service name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required")
                .MaximumLength(1000)
                .WithMessage("Description cannot exceed 1000 characters");

            RuleFor(x => x.CategoryName)
                .NotEmpty()
                .WithMessage("Category is required");

            RuleFor(x => x.DurationMinutes)
                .GreaterThan(0)
                .WithMessage("Duration must be greater than 0 minutes")
                .LessThanOrEqualTo(480)
                .WithMessage("Duration cannot exceed 8 hours");

            When(x => x.PreparationMinutes.HasValue, () =>
            {
                RuleFor(x => x.PreparationMinutes)
                    .GreaterThan(0)
                    .WithMessage("Preparation time must be greater than 0")
                    .LessThanOrEqualTo(120)
                    .WithMessage("Preparation time cannot exceed 2 hours");
            });

            When(x => x.BufferMinutes.HasValue, () =>
            {
                RuleFor(x => x.BufferMinutes)
                    .GreaterThan(0)
                    .WithMessage("Buffer time must be greater than 0")
                    .LessThanOrEqualTo(60)
                    .WithMessage("Buffer time cannot exceed 1 hour");
            });

         

            When(x => !string.IsNullOrEmpty(x.ImageUrl), () =>
            {
                RuleFor(x => x.ImageUrl)
                    .Must(BeValidUrl)
                    .WithMessage("Image URL must be a valid URL");
            });
        }

        private static bool BeValidUrl(string? url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var result)
                   && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }
    }
}