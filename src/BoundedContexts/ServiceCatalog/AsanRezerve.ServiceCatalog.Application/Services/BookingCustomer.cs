using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Identity;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.Repositories;

namespace AsanRezerve.ServiceCatalog.Application.Services
{
    /// <summary>
    /// Who a booking is FOR. A booking the customer made is theirs. A booking the salon entered (a walk-in or a phone
    /// booking) stores the salon's own owner as its customer; the person it is for is the salon's client-book entry,
    /// named by mobile number — and sign-in proves the number, so what a salon recorded against it is that person's
    /// (openspec/changes/_inline/customer-sees-salon-bookings). One rule for seeing such a booking in «نوبت‌های من»,
    /// opening it, and reviewing it (openspec/changes/_inline/customer-reviews-and-nahal-seed).
    /// </summary>
    public interface IBookingCustomer
    {
        /// <summary>
        /// The client-book entries, at any salon, that are this person: the ones carrying their verified mobile. Empty
        /// when their number cannot be read.
        /// </summary>
        Task<IReadOnlyCollection<Guid>> TheirBookEntriesAsync(Guid personId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether this booking is for this person. For a salon-entered booking that is never the salon owner who is
        /// stored as its customer — a salon does not review itself.
        /// </summary>
        Task<bool> IsForAsync(Booking booking, Guid personId, CancellationToken cancellationToken = default);
    }

    /// <inheritdoc />
    public sealed class BookingCustomer : IBookingCustomer
    {
        private readonly IPersonDirectory _people;
        private readonly IProviderCustomerRepository _providerCustomers;

        public BookingCustomer(IPersonDirectory people, IProviderCustomerRepository providerCustomers)
        {
            _people = people;
            _providerCustomers = providerCustomers;
        }

        public async Task<IReadOnlyCollection<Guid>> TheirBookEntriesAsync(
            Guid personId, CancellationToken cancellationToken = default)
        {
            var people = await _people.FindByIdsAsync(new[] { personId }, cancellationToken);
            if (!people.TryGetValue(personId, out var person) || string.IsNullOrWhiteSpace(person.PhoneNumber))
                return Array.Empty<Guid>();

            try
            {
                return await _providerCustomers.IdsByPhoneAsync(PhoneNumber.From(person.PhoneNumber!), cancellationToken);
            }
            catch (ArgumentException)
            {
                return Array.Empty<Guid>();
            }
        }

        public async Task<bool> IsForAsync(Booking booking, Guid personId, CancellationToken cancellationToken = default) =>
            booking.ProviderCustomerId is null
                ? IsFor(booking, personId, Array.Empty<Guid>())
                : IsFor(booking, personId, await TheirBookEntriesAsync(personId, cancellationToken));

        /// <summary>
        /// The rule, given the person's own client-book entries: a booking the salon entered is for the entry's
        /// person — never for the owner stored as its customer, whose «نوبت‌های من» lists it all the same.
        /// </summary>
        public static bool IsFor(Booking booking, Guid personId, IReadOnlyCollection<Guid> theirBookEntries) =>
            booking.ProviderCustomerId is { } entryId
                ? theirBookEntries.Contains(entryId)
                : booking.CustomerId.Value == personId;
    }

    /// <summary>
    /// Where a booking's review stands, for the person the booking is for: whether they may write one now, why not
    /// yet, or the one they already wrote. Said up front so the apps offer «ثبت نظر», disable it with the reason, or
    /// show the review — instead of offering it on status alone and failing on the second try.
    /// </summary>
    public sealed record BookingReviewStanding(
        bool CanReview,
        string? ReviewBlockedReason,
        Guid? ReviewId,
        string? ReviewStatus)
    {
        /// <summary>For anyone the booking is not for: nothing to offer, nothing to say.</summary>
        public static readonly BookingReviewStanding None = new(false, null, null, null);

        public static BookingReviewStanding Of(Booking booking, BookingReviewState? review) =>
            review is not null
                ? new BookingReviewStanding(false, null, review.ReviewId, review.ModerationStatus.ToString())
                : new BookingReviewStanding(booking.CanBeReviewed(), booking.ReviewBlockedReason(), null, null);
    }
}
