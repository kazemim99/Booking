// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/RecordProviderVisit/RecordProviderVisitCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.RecordProviderVisit
{
    /// <summary>
    /// Validator for RecordProviderVisitCommand
    /// </summary>
    public sealed class RecordProviderVisitCommandValidator : AbstractValidator<RecordProviderVisitCommand>
    {
        public RecordProviderVisitCommandValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty()
                .WithMessage("Customer ID is required");

            RuleFor(x => x.ProviderId)
                .NotEmpty()
                .WithMessage("Provider ID is required");

            RuleFor(x => x.ViewSource)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.ViewSource))
                .WithMessage("View source must not exceed 50 characters");
        }
    }
}
