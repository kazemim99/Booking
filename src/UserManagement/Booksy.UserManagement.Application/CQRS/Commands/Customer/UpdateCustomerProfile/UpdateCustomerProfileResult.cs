// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/UpdateCustomerProfile/UpdateCustomerProfileResult.cs
// ========================================

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.UpdateCustomerProfile
{
    /// <summary>
    /// Result of updating customer profile
    /// </summary>
    public sealed record UpdateCustomerProfileResult(
        Guid CustomerId,
        string FullName,
        DateTime UpdatedAt);
}
