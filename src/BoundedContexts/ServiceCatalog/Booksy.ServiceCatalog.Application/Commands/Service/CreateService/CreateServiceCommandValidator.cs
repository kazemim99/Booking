// ========================================
// Booksy.ServiceCatalog.Application/Commands/Service/CreateService/CreateServiceCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Service.CreateService
{
    public sealed class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
    {
        public CreateServiceCommandValidator()
        {
            RuleFor(x => x.ProviderId)
                .NotEmpty()
                .WithMessage("Provider ID is required");

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

            RuleFor(x => x.ServiceType)
                .IsInEnum()
                .WithMessage("Valid service type is required");

            RuleFor(x => x.BasePrice)
                .GreaterThan(0)
                .WithMessage("Base price must be greater than 0");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage("Currency is required")
                .Length(3)
                .WithMessage("Currency must be 3 characters (ISO code)");

            RuleFor(x => x.DurationMinutes)
                .GreaterThan(0)
                .WithMessage("Duration must be greater than 0 minutes")
                .LessThanOrEqualTo(480)
                .WithMessage("Duration cannot exceed 8 hours");

            When(x => x.PreparationMinutes.HasValue, () =>
            {
                RuleFor(x => x.PreparationMinutes)
                    .GreaterThan(0)
                    .WithMessage("Preparation time must be greater than 0");
            });

            When(x => x.BufferMinutes.HasValue, () =>
            {
                RuleFor(x => x.BufferMinutes)
                    .GreaterThan(0)
                    .WithMessage("Buffer time must be greater than 0");
            });

            When(x => x.RequiresDeposit, () =>
            {
                RuleFor(x => x.DepositPercentage)
                    .GreaterThan(0)
                    .WithMessage("Deposit percentage must be greater than 0")
                    .LessThanOrEqualTo(100)
                    .WithMessage("Deposit percentage cannot exceed 100");
            });

            RuleFor(x => x.MaxAdvanceBookingDays)
                .GreaterThan(0)
                .WithMessage("Max advance booking days must be greater than 0");

            RuleFor(x => x.MinAdvanceBookingHours)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Min advance booking hours cannot be negative");

            RuleFor(x => x.MaxConcurrentBookings)
                .GreaterThan(0)
                .WithMessage("Max concurrent bookings must be greater than 0");
        }
    }
}