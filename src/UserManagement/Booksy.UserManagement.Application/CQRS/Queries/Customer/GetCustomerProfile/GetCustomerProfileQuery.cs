// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetCustomerProfile/GetCustomerProfileQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerProfile
{
    /// <summary>
    /// Query to get customer profile including notification preferences
    /// </summary>
    public sealed record GetCustomerProfileQuery : IQuery<CustomerProfileViewModel>
    {
        public Guid CustomerId { get; init; }

        public GetCustomerProfileQuery(Guid customerId)
        {
            CustomerId = customerId;
        }
    }
}
