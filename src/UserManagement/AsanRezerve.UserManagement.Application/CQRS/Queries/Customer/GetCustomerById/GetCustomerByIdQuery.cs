// ========================================
// AsanRezerve.UserManagement.Application/CQRS/Queries/Customer/GetCustomerById/GetCustomerByIdQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.UserManagement.Application.CQRS.Queries.Customer.GetCustomerById
{
    public sealed record GetCustomerByIdQuery(Guid CustomerId) : IQuery<CustomerDetailsViewModel>
    {
        public bool IsCacheable => true;
        public string CacheKey => $"customer:details:{CustomerId}";
        public int CacheExpirationSeconds => 300; // 5 minutes
    }
}
