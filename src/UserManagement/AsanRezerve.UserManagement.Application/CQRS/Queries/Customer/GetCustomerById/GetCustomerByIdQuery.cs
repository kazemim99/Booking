// ========================================
// AsanRezerve.UserManagement.Application/CQRS/Queries/Customer/GetCustomerById/GetCustomerByIdQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.UserManagement.Application.CQRS.Queries.Customer.GetCustomerById
{
    // Not cacheable: personal data its owner edits, and nothing evicted it — profile edits did not show
    // (add-observability-and-caching).
    public sealed record GetCustomerByIdQuery(Guid CustomerId) : IQuery<CustomerDetailsViewModel>;
}
