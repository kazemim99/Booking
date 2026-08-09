using Booksy.Core.Application.Authorization;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Authorization
{
    /// <summary>
    /// Resolves the owning identities of the booking targeted by any command that
    /// implements <see cref="IRequireBookingOwnership"/>: the booking's customer and
    /// the owning provider's owner-user. One generic resolver, registered per command.
    /// </summary>
    public sealed class BookingOwnershipResolver<TCommand> : IResourceOwnershipResolver<TCommand>
        where TCommand : IRequireBookingOwnership
    {
        private readonly IBookingWriteRepository _bookings;
        private readonly IProviderReadRepository _providers;

        public BookingOwnershipResolver(
            IBookingWriteRepository bookings,
            IProviderReadRepository providers)
        {
            _bookings = bookings;
            _providers = providers;
        }

        public async Task<ResourceOwners?> ResolveAsync(TCommand command, CancellationToken cancellationToken)
        {
            var booking = await _bookings.GetByIdAsync(BookingId.From(command.BookingId), cancellationToken);
            if (booking is null)
                return null; // not found → fail closed (behavior denies)

            var provider = await _providers.GetByIdAsync(booking.ProviderId, cancellationToken);
            return new ResourceOwners(booking.CustomerId.Value, provider?.OwnerId.Value);
        }
    }

    /// <summary>
    /// Resolves the owning identities of the payment targeted by any command that
    /// implements <see cref="IRequirePaymentOwnership"/>: the payment's customer and
    /// the owning provider's owner-user.
    /// </summary>
    public sealed class PaymentOwnershipResolver<TCommand> : IResourceOwnershipResolver<TCommand>
        where TCommand : IRequirePaymentOwnership
    {
        private readonly IPaymentWriteRepository _payments;
        private readonly IProviderReadRepository _providers;

        public PaymentOwnershipResolver(
            IPaymentWriteRepository payments,
            IProviderReadRepository providers)
        {
            _payments = payments;
            _providers = providers;
        }

        public async Task<ResourceOwners?> ResolveAsync(TCommand command, CancellationToken cancellationToken)
        {
            var payment = await _payments.GetByIdAsync(PaymentId.From(command.PaymentId), cancellationToken);
            if (payment is null)
                return null;

            var provider = await _providers.GetByIdAsync(payment.ProviderId, cancellationToken);
            return new ResourceOwners(payment.CustomerId.Value, provider?.OwnerId.Value);
        }
    }

    /// <summary>
    /// Resolves the owning identity of the provider targeted by any command implementing
    /// <see cref="IRequireProviderOwnership"/> — used for business-settings changes such as the booking/deposit
    /// policy. There is no customer owner for a provider resource, so only the provider's owner-user is returned:
    /// a customer can never satisfy the ownership check, and an unknown provider fails closed.
    /// </summary>
    public sealed class ProviderOwnershipResolver<TCommand> : IResourceOwnershipResolver<TCommand>
        where TCommand : IRequireProviderOwnership
    {
        private readonly IProviderReadRepository _providers;

        public ProviderOwnershipResolver(IProviderReadRepository providers) => _providers = providers;

        public async Task<ResourceOwners?> ResolveAsync(TCommand command, CancellationToken cancellationToken)
        {
            var provider = await _providers.GetByIdAsync(ProviderId.From(command.ProviderId), cancellationToken);
            if (provider is null)
                return null; // not found → fail closed (behavior denies)

            return new ResourceOwners(CustomerId: null, ProviderOwnerUserId: provider.OwnerId.Value);
        }
    }
}
