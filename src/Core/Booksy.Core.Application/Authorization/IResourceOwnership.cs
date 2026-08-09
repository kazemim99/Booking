namespace Booksy.Core.Application.Authorization
{
    /// <summary>
    /// Marks a command that mutates a resource owned by a specific customer and/or
    /// provider. The <see cref="AuthorizationBehavior{TRequest,TResponse}"/> enforces
    /// that the authenticated caller owns the resource (or holds an authorized role)
    /// before the handler runs. The acting identity is always server-derived — never
    /// bound from the request body.
    /// </summary>
    public interface IRequireResourceOwnership
    {
        /// <summary>
        /// The authenticated caller's id, populated server-side by the controller
        /// from the JWT. Used for audit; authorization itself resolves identity from
        /// <c>ICurrentUserService</c> so a spoofed value cannot grant access.
        /// </summary>
        Guid ActingUserId { get; }
    }

    /// <summary>Ownership over a booking resource.</summary>
    public interface IRequireBookingOwnership : IRequireResourceOwnership
    {
        Guid BookingId { get; }
    }

    /// <summary>Ownership over a payment resource.</summary>
    public interface IRequirePaymentOwnership : IRequireResourceOwnership
    {
        Guid PaymentId { get; }
    }

    /// <summary>
    /// Ownership over a provider (business) resource — used by settings changes such as the booking/deposit policy,
    /// where only the provider's owner (or an admin) may make the change.
    /// </summary>
    public interface IRequireProviderOwnership : IRequireResourceOwnership
    {
        Guid ProviderId { get; }
    }
}
