// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/RemoveFavoriteProvider/RemoveFavoriteProviderCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.RemoveFavoriteProvider
{
    /// <summary>
    /// Validator for RemoveFavoriteProviderCommand
    /// </summary>
    public sealed class RemoveFavoriteProviderCommandValidator : AbstractValidator<RemoveFavoriteProviderCommand>
    {
        public RemoveFavoriteProviderCommandValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("CustomerId is required");

            RuleFor(x => x.ProviderId)
                .NotEmpty().WithMessage("ProviderId is required");
        }
    }
}
