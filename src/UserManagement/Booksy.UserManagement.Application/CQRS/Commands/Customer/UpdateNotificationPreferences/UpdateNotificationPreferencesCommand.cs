// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/UpdateNotificationPreferences/UpdateNotificationPreferencesCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.UpdateNotificationPreferences
{
    /// <summary>
    /// Command to update customer notification preferences
    /// </summary>
    public sealed record UpdateNotificationPreferencesCommand : ICommand<UpdateNotificationPreferencesResult>
    {
        public Guid CustomerId { get; init; }
        public bool SmsEnabled { get; init; }
        public bool EmailEnabled { get; init; }
        public string ReminderTiming { get; init; } = "24h";
        public Guid? IdempotencyKey { get; init; }
    }
}
