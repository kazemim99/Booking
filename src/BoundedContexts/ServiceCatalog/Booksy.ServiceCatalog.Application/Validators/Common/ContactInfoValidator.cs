// ========================================
// Booksy.ServiceCatalog.Application/Specifications/Service/ContactInfoValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Validators.Common
{
    /// <summary>
    /// Validator for ContactInfo value object
    /// </summary>
    public sealed class ContactInfoValidator : AbstractValidator<ContactInfo>
    {
        public ContactInfoValidator()
        {
      

            RuleFor(x => x.PrimaryPhone)
                .NotNull()
                .WithMessage("Phone number is required");

            When(x => x.SecondaryPhone != null, () => {
                RuleFor(x => x.SecondaryPhone)
                    .NotNull()
                    .WithMessage("Secondary phone must be valid if provided");
            });
        }
    }
}