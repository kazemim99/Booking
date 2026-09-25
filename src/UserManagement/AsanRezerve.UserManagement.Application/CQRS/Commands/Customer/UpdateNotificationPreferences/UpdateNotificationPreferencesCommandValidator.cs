// ========================================
// AsanRezerve.UserManagement.Application/CQRS/Commands/Customer/UpdateNotificationPreferences/UpdateNotificationPreferencesCommandValidator.cs
// ========================================
using FluentValidation;

namespace AsanRezerve.UserManagement.Application.CQRS.Commands.Customer.UpdateNotificationPreferences
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

            // Deliberately NO "at least one channel" rule. The customer-profile spec gives the
            // customer two independent toggles and the settings modal warns when both are off
            // (SettingsModal.vue), and notification-delivery keeps a documented set of
            // non-suppressible notifications for exactly that state. Refusing the request here
            // contradicted both and made the "disable all" path a 400.
        }
    }
}
