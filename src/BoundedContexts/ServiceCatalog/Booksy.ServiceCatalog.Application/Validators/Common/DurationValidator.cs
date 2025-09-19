// ========================================
// Booksy.ServiceCatalog.Application/Specifications/Service/DurationValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Validators.Common
{
    /// <summary>
    /// Validator for Duration value object
    /// </summary>
    public sealed class DurationValidator : AbstractValidator<Duration>
    {
        public DurationValidator()
        {
            RuleFor(x => x.Value)
                .GreaterThan(0)
                .WithMessage("Duration must be greater than zero minutes")
                .LessThanOrEqualTo(1440) // 24 hours
                .WithMessage("Duration cannot exceed 1440 minutes (24 hours)");
        }
    }
}