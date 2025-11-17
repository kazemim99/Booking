// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetCustomerProfile/CustomerProfileViewModel.cs
// ========================================

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerProfile
{
    /// <summary>
    /// View model for customer profile
    /// </summary>
    public sealed record CustomerProfileViewModel
    {
        public Guid CustomerId { get; init; }
        public Guid UserId { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string? Email { get; init; }
        public string? PhoneNumber { get; init; }

        // Notification Preferences
        public bool SmsEnabled { get; init; }
        public bool EmailEnabled { get; init; }
        public string ReminderTiming { get; init; } = string.Empty;

        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
