// ========================================
// AsanRezerve.UserManagement.Application/CQRS/Queries/Customer/GetCustomerProfile/GetCustomerProfileQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.UserManagement.Application.CQRS.Queries.Customer.GetCustomerProfile
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
