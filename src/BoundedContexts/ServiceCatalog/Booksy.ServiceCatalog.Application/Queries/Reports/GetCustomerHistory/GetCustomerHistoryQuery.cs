// ========================================
// Booksy.ServiceCatalog.Application/Queries/Reports/GetCustomerHistory/GetCustomerHistoryQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Reports.GetCustomerHistory
{
    public sealed record GetCustomerHistoryQuery(Guid CustomerId) : IQuery<CustomerHistoryDto>;
}
