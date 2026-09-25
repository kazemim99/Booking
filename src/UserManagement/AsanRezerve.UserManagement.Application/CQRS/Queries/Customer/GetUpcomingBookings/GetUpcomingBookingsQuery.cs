// ========================================
// AsanRezerve.UserManagement.Application/CQRS/Queries/Customer/GetUpcomingBookings/GetUpcomingBookingsQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.UserManagement.Application.CQRS.Queries.Customer.GetUpcomingBookings
{
    /// <summary>
    /// Query to get customer's upcoming bookings (next 5)
    /// </summary>
    public sealed record GetUpcomingBookingsQuery : IQuery<List<UpcomingBookingViewModel>>
    {
        public Guid CustomerId { get; init; }
        public int Limit { get; init; } = 5;

        public GetUpcomingBookingsQuery(Guid customerId, int limit = 5)
        {
            CustomerId = customerId;
            Limit = limit;
        }
    }
}
