// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/AddFavoriteProvider/AddFavoriteProviderCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.AddFavoriteProvider
{
    /// <summary>
    /// Validator for AddFavoriteProviderCommand
    /// </summary>
    public sealed class AddFavoriteProviderCommandValidator : AbstractValidator<AddFavoriteProviderCommand>
    {
        public AddFavoriteProviderCommandValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("CustomerId is required");

            RuleFor(x => x.ProviderId)
                .NotEmpty().WithMessage("ProviderId is required");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Notes));
        }
    }
}
