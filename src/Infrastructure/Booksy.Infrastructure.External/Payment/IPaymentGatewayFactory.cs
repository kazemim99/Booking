using Booksy.Core.Domain.Enums;

namespace Booksy.Infrastructure.External.Payment
{
    /// <summary>
    /// Abstract Factory for creating payment gateway instances based on provider
    /// </summary>
    public interface IPaymentGatewayFactory
    {
        /// <summary>
        /// Creates and returns a payment gateway instance for the specified provider
        /// </summary>
        /// <param name="provider">The payment provider type</param>
        /// <returns>An instance of IPaymentGateway for the specified provider</returns>
        /// <exception cref="ArgumentException">Thrown when the provider is not supported</exception>
        IPaymentGateway CreatePaymentGateway(PaymentProvider provider);

        /// <summary>
        /// Gets the default payment gateway based on configuration
        /// </summary>
        /// <returns>An instance of the default IPaymentGateway</returns>
        IPaymentGateway GetDefaultGateway();

        /// <summary>
        /// Checks if a specific payment provider is supported and configured
        /// </summary>
        /// <param name="provider">The payment provider to check</param>
        /// <returns>True if the provider is supported, false otherwise</returns>
        bool IsProviderSupported(PaymentProvider provider);

        /// <summary>
        /// Gets all supported payment providers
        /// </summary>
        /// <returns>Collection of supported payment providers</returns>
        IEnumerable<PaymentProvider> GetSupportedProviders();
    }
}
