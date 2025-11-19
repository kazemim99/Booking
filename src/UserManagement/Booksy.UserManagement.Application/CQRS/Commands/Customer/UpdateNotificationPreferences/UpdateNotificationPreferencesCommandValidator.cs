// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/UpdateNotificationPreferences/UpdateNotificationPreferencesCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.UpdateNotificationPreferences
{
    /// <summary>
    /// Validator for UpdateNotificationPreferencesCommand
    /// </summary>
    public sealed class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
    {
        private static readonly string[] ValidTimings = new[] { "1h", "24h", "3d" };

        public UpdateNotificationPreferencesCommandValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("CustomerId is required");

            RuleFor(x => x.ReminderTiming)
                .NotEmpty().WithMessage("ReminderTiming is required")
                .Must(t => ValidTimings.Contains(t))
                .WithMessage($"ReminderTiming must be one of: {string.Join(", ", ValidTimings)}");

            RuleFor(x => x)
                .Must(x => x.SmsEnabled || x.EmailEnabled)
                .WithMessage("At least one notification channel (SMS or Email) must be enabled");
        }
    }
}
