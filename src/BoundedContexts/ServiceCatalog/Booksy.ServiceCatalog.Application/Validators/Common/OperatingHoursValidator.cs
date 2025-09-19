// ========================================
// Booksy.ServiceCatalog.Application/Specifications/Service/OperatingHoursValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Validators.Common
{
    /// <summary>
    /// Validator for OperatingHours value object
    /// </summary>
    public sealed class OperatingHoursValidator : AbstractValidator<OperatingHours>
    {
        public OperatingHoursValidator()
        {
            RuleFor(x => x.StartTime)
                .NotEmpty()
                .WithMessage("Start time is required")
                .Must(BeValidTime)
                .WithMessage("Start time must be a valid time");

            RuleFor(x => x.EndTime)
                .NotEmpty()
                .WithMessage("End time is required")
                .Must(BeValidTime)
                .WithMessage("End time must be a valid time");

            RuleFor(x => x)
                .Must(HaveValidTimeRange)
                .WithMessage("End time must be after start time")
                .When(x => x.StartTime != default && x.EndTime != default);

            RuleFor(x => x)
                .Must(HaveReasonableWorkingHours)
                .WithMessage("Working hours cannot exceed 16 hours per day")
                .When(x => x.StartTime != default && x.EndTime != default);

            RuleFor(x => x)
                .Must(HaveValidBreakTimes)
                .WithMessage("Break times must be within working hours")
                .When(x => HasBreakTimes(x));

            When(x => HasBreakTimes(x), () => {
                RuleFor(x => x)
                    .Must(HaveValidBreakDuration)
                    .WithMessage("Break duration cannot exceed 3 hours");
            });
        }

        private static bool BeValidTime(TimeOnly time)
        {
            return time >= TimeOnly.MinValue && time <= TimeOnly.MaxValue;
        }

        private static bool HaveValidTimeRange(OperatingHours hours)
        {
            // Handle normal working hours (e.g., 09:00 to 17:00)
            if (hours.EndTime > hours.StartTime)
                return true;

            // Handle overnight shifts (e.g., 22:00 to 06:00)
            return hours.EndTime < hours.StartTime;
        }

        private static bool HaveReasonableWorkingHours(OperatingHours hours)
        {
            TimeSpan duration;

            if (hours.EndTime > hours.StartTime)
            {
                // Normal day shift
                duration = hours.EndTime.ToTimeSpan() - hours.StartTime.ToTimeSpan();
            }
            else
            {
                // Overnight shift
                duration = (TimeSpan.FromDays(1) - hours.StartTime.ToTimeSpan()) + hours.EndTime.ToTimeSpan();
            }

            return duration <= TimeSpan.FromHours(16);
        }

        private static bool HasBreakTimes(OperatingHours hours)
        {
            // This would check if the OperatingHours has break times defined
            // Since the domain model doesn't show break times in OperatingHours,
            // this is a placeholder
            return false;
        }

        private static bool HaveValidBreakTimes(OperatingHours hours)
        {
            // Placeholder for break time validation
            return true;
        }

        private static bool HaveValidBreakDuration(OperatingHours hours)
        {
            // Placeholder for break duration validation
            return true;
        }
    }
}

