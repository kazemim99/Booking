using Booksy.UserManagement.Domain.ReadModels;

namespace Booksy.UserManagement.Domain.Repositories
{
    /// <summary>
    /// Read side of the customer booking history read model (<see cref="CustomerBookingHistoryEntry"/>),
    /// which <c>BookingEventSubscribers</c> fills from ServiceCatalog booking events. Until this
    /// existed both customer booking queries were stubs that always returned an empty list.
    /// </summary>
    public interface ICustomerBookingHistoryReadRepository
    {
        /// <summary>
        /// Bookings that have not started yet and are not cancelled, soonest first.
        /// </summary>
        Task<IReadOnlyList<CustomerBookingHistoryEntry>> GetUpcomingAsync(
            Guid customerId,
            DateTime nowUtc,
            int limit,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// One page of the customer's whole history, most recent start first, with the total count.
        /// </summary>
        Task<(IReadOnlyList<CustomerBookingHistoryEntry> Items, int TotalCount)> GetHistoryPageAsync(
            Guid customerId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
