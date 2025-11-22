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
                .MaximumLength(1000)
                .WithMessage("Description cannot exceed 1000 characters");

         
        }
      
    }
}