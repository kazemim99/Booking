// ========================================
// Booksy.UserManagement.Domain/Events/NotificationPreferencesUpdatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates.CustomerAggregate;

namespace Booksy.UserManagement.Domain.Events
{
    /// <summary>
    /// Domain event raised when customer notification preferences are updated
    /// </summary>
    public sealed record NotificationPreferencesUpdatedEvent(
        CustomerId CustomerId,
        UserId UserId,
        bool SmsEnabled,
        bool EmailEnabled,
        string ReminderTiming,
        DateTime UpdatedAt) : DomainEvent("Customer", CustomerId.ToString());
}
