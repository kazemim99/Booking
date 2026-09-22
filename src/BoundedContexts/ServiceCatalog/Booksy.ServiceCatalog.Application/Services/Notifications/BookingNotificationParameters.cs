using Booksy.ServiceCatalog.Application.Abstractions.Identity;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Repositories;

namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// The inputs every booking notification is written from: the salon, the time, the service and the customer's
    /// name — decided in one place, so a salon is never told about «مشتری گرامی» when the booking has a real name.
    /// </summary>
    public interface IBookingNotificationParameters
    {
        /// <param name="businessName">The salon's name when the caller already holds it; looked up otherwise.</param>
        /// <param name="bookedFor">The walk-in the booking was made for, when the caller holds it — needed while the
        /// customer-book entry is still unsaved in the same unit of work.</param>
        Task<Dictionary<string, string>> ForAsync(
            Booking booking,
            string? businessName = null,
            CancellationToken cancellationToken = default,
            Domain.Aggregates.ProviderCustomer? bookedFor = null);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Found in the QA walkthrough of 2026-09-22: every booking notification read «مشتری گرامی», because no raise
    /// site passed a customer name (the reminder scheduler declared one and always left it null), and none passed
    /// the service. The name is captured into the intent now, like the rest: the sweep must never re-read it.
    /// </remarks>
    public sealed class BookingNotificationParameters : IBookingNotificationParameters
    {
        private readonly IProviderReadRepository _providers;
        private readonly IServiceReadRepository _services;
        private readonly IProviderCustomerRepository _providerCustomers;
        private readonly IPersonDirectory _people;

        public BookingNotificationParameters(
            IProviderReadRepository providers,
            IServiceReadRepository services,
            IProviderCustomerRepository providerCustomers,
            IPersonDirectory people)
        {
            _providers = providers;
            _services = services;
            _providerCustomers = providerCustomers;
            _people = people;
        }

        public async Task<Dictionary<string, string>> ForAsync(
            Booking booking,
            string? businessName = null,
            CancellationToken cancellationToken = default,
            Domain.Aggregates.ProviderCustomer? bookedFor = null)
        {
            ArgumentNullException.ThrowIfNull(booking);

            businessName ??= (await _providers.GetByIdAsync(booking.ProviderId, cancellationToken))?.Profile.BusinessName;
            var service = await _services.GetByIdAsync(booking.ServiceId, cancellationToken);

            var parameters = new Dictionary<string, string>
            {
                [NotificationParameter.BusinessName] = string.IsNullOrWhiteSpace(businessName) ? "سالن" : businessName,
                [NotificationParameter.StartTime] = booking.TimeSlot.StartTime.ToString("o"),
            };

            if (!string.IsNullOrWhiteSpace(service?.Name))
                parameters[NotificationParameter.ServiceName] = service!.Name;

            var name = bookedFor is not null
                ? PersonName.RealOrNull(bookedFor.FirstName, bookedFor.LastName)
                : await CustomerNameAsync(booking, cancellationToken);
            if (name is not null)
                parameters[NotificationParameter.CustomerName] = name;

            return parameters;
        }

        /// <summary>
        /// A walk-in is named from the salon's own customer book — for one, the aggregate's customer is the salon's
        /// owner. Anyone else is the person who booked. A placeholder name is no name.
        /// </summary>
        private async Task<string?> CustomerNameAsync(Booking booking, CancellationToken cancellationToken)
        {
            if (booking.ProviderCustomerId is { } providerCustomerId)
            {
                var walkIn = await _providerCustomers.GetAsync(booking.ProviderId, providerCustomerId, cancellationToken);
                return walkIn is null ? null : PersonName.RealOrNull(walkIn.FirstName, walkIn.LastName);
            }

            var people = await _people.FindByIdsAsync(new[] { booking.CustomerId.Value }, cancellationToken);
            return people.TryGetValue(booking.CustomerId.Value, out var person)
                ? PersonName.RealOrNull(person.FirstName, person.LastName)
                : null;
        }
    }

}
