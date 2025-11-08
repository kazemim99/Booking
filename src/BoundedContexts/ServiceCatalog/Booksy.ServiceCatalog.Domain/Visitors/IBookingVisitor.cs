// ========================================
// Booksy.ServiceCatalog.Domain/Visitors/IBookingVisitor.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;

namespace Booksy.ServiceCatalog.Domain.Visitors
{
    /// <summary>
    /// Visitor pattern interface for processing booking-related aggregates.
    /// Allows separation of reporting/analysis logic from domain entities.
    /// </summary>
    /// <typeparam name="TResult">The type of result produced by the visitor</typeparam>
    public interface IBookingVisitor<TResult>
    {
        /// <summary>
        /// Visit a Booking aggregate
        /// </summary>
        /// <param name="booking">The booking to visit</param>
        void Visit(Booking booking);

        /// <summary>
        /// Visit a Payment aggregate
        /// </summary>
        /// <param name="payment">The payment to visit</param>
        void Visit(Payment payment);

        /// <summary>
        /// Get the accumulated result after visiting aggregates
        /// </summary>
        /// <returns>The visitor's result</returns>
        TResult GetResult();
    }
}
