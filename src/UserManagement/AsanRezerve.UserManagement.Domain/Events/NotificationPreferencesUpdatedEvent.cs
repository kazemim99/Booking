// ========================================
// AsanRezerve.UserManagement.Domain/Events/NotificationPreferencesUpdatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Domain.Aggregates.CustomerAggregate;

namespace AsanRezerve.UserManagement.Domain.Events
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
