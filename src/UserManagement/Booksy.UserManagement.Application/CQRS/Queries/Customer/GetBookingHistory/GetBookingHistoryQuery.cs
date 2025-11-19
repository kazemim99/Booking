// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetBookingHistory/GetBookingHistoryQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetBookingHistory
{
    /// <summary>
    /// Query to get customer's booking history (paginated)
    /// </summary>
    public sealed record GetBookingHistoryQuery : IQuery<BookingHistoryResult>
    {
        public Guid CustomerId { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        public GetBookingHistoryQuery(Guid customerId, int page = 1, int pageSize = 20)
        {
            CustomerId = customerId;
            Page = page;
            PageSize = pageSize;
        }
    }
}
