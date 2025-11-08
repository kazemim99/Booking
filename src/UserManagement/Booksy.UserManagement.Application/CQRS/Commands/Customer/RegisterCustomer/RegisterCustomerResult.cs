// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/RegisterCustomer/RegisterCustomerResult.cs
// ========================================

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.RegisterCustomer
{
    /// <summary>
    /// Result of customer registration
    /// </summary>
    public sealed record RegisterCustomerResult(
        Guid CustomerId,
        Guid UserId,
        string Email,
        string FullName,
        string PhoneNumber,
        DateTime RegisteredAt);
}
