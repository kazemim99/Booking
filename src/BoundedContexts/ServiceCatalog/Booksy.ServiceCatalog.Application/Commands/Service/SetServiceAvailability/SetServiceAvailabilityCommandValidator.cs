// ========================================
// Booksy.ServiceCatalog.Application/Commands/Service/SetServiceAvailability/SetServiceAvailabilityCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Service.SetServiceAvailability
{
    public sealed class SetServiceAvailabilityCommandValidator : AbstractValidator<SetServiceAvailabilityCommand>
    {
        public SetServiceAvailabilityCommandValidator()
        {
            RuleFor(x => x.ServiceId)
                .NotEmpty()
                .WithMessage("Service ID is required");

            RuleFor(x => x.Availability)
                .NotNull()
                .WithMessage("Availability is required")
                .Must(availability => availability.Count <= 7)
                .WithMessage("Cannot set availability for more than 7 days");

            RuleForEach(x => x.Availability)
                .Must(ValidateAvailability)
                .WithMessage("Invalid availability - start time must be before end time");
        }

        private static bool ValidateAvailability(KeyValuePair<Domain.Enums.DayOfWeek, ServiceAvailabilityRequest?> availability)
        {
            if (availability.Value == null) return true; // Unavailable day is valid

            var request = availability.Value;

            if (request.StartTime >= request.EndTime) return false;
            if (request.MaxConcurrentBookings <= 0) return false;

            return true;
        }
    }
}