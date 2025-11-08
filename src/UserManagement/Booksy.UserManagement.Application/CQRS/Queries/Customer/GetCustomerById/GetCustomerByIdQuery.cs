// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetCustomerById/GetCustomerByIdQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerById
{
    public sealed record GetCustomerByIdQuery(Guid CustomerId) : IQuery<CustomerDetailsViewModel>
    {
        public bool IsCacheable => true;
        public string CacheKey => $"customer:details:{CustomerId}";
        public int CacheExpirationSeconds => 300; // 5 minutes
    }
}
