// ========================================
// PaymentGatewayFactory.cs
// ========================================
using Booksy.Core.Application.Abstractions.Payment;
using Booksy.Infrastructure.External.Payment.Behpardakht;
using Booksy.Infrastructure.External.Payment.IDPay;
using Booksy.Infrastructure.External.Payment.Parsian;
using Booksy.Infrastructure.External.Payment.Saman;
using Booksy.Infrastructure.External.Payment.ZarinPal;
using Booksy.ServiceCatalog.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.External.Payment
{
    /// <summary>
    /// Factory implementation for creating payment gateway instances
    /// Implements the Abstract Factory pattern for payment gateway creation
    /// </summary>
    public class PaymentGatewayFactory : IPaymentGatewayFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentGatewayFactory> _logger;

        public PaymentGatewayFactory(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<PaymentGatewayFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a payment gateway instance for the specified provider
        /// </summary>
        public IPaymentGateway CreatePaymentGateway(PaymentProvider provider)
        {
            _logger.LogInformation("Creating payment gateway for provider: {Provider}", provider);

            if (!IsProviderSupported(provider))
            {
                _logger.LogError("Payment provider {Provider} is not supported or not configured", provider);
                throw new ArgumentException($"Payment provider {provider} is not supported or not configured", nameof(provider));
            }

            try
            {
                var gateway = provider switch
                {
                    PaymentProvider.ZarinPal => GetService<ZarinPalPaymentGateway>(),
                    PaymentProvider.IDPay => GetService<IDPayPaymentGateway>(),
                    PaymentProvider.Behpardakht => GetService<BehpardakhtPaymentGateway>(),
                    PaymentProvider.Parsian => GetService<ParsianPaymentGateway>(),
                    PaymentProvider.Saman => GetService<SamanPaymentGateway>(),
                    _ => throw new ArgumentException($"Unknown payment provider: {provider}", nameof(provider))
                };

                _logger.LogInformation("Successfully created payment gateway for provider: {Provider}", provider);
                return gateway;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create payment gateway for provider: {Provider}", provider);
                throw;
            }
        }

        /// <summary>
        /// Gets the default payment gateway based on configuration
        /// </summary>
        public IPaymentGateway GetDefaultGateway()
        {
            var defaultProviderName = _configuration["Payment:DefaultProvider"] ?? "ZarinPal";

            if (!Enum.TryParse<PaymentProvider>(defaultProviderName, true, out var defaultProvider))
            {
                _logger.LogWarning("Invalid default provider '{Provider}' in configuration. Falling back to ZarinPal", defaultProviderName);
                defaultProvider = PaymentProvider.ZarinPal;
            }

            _logger.LogInformation("Using default payment provider: {Provider}", defaultProvider);
            return CreatePaymentGateway(defaultProvider);
        }

        /// <summary>
        /// Checks if a specific payment provider is supported and configured
        /// </summary>
        public bool IsProviderSupported(PaymentProvider provider)
        {
            // Check if the provider is enabled in configuration
            var providerSection = _configuration.GetSection($"Payment:{provider}");

            if (!providerSection.Exists())
            {
                _logger.LogDebug("Provider {Provider} configuration section not found", provider);
                return false;
            }

            // Check if provider is explicitly disabled
            var isEnabled = providerSection.GetValue<bool?>("Enabled");
            if (isEnabled.HasValue && !isEnabled.Value)
            {
                _logger.LogDebug("Provider {Provider} is explicitly disabled in configuration", provider);
                return false;
            }

            // Placeholder gateways (Parsian, Saman) are considered "supported" but will throw NotImplementedException
            // This allows for future implementation without code changes
            if (provider == PaymentProvider.Parsian || provider == PaymentProvider.Saman)
            {
                _logger.LogDebug("Provider {Provider} is a placeholder implementation", provider);
                return true;
            }

            // For other providers, check if required configuration exists
            return provider switch
            {
                PaymentProvider.ZarinPal => !string.IsNullOrEmpty(providerSection["MerchantId"]),
                PaymentProvider.IDPay => !string.IsNullOrEmpty(providerSection["ApiKey"]),
                PaymentProvider.Behpardakht => !string.IsNullOrEmpty(providerSection["TerminalId"]),
                _ => false
            };
        }

        /// <summary>
        /// Gets all supported payment providers
        /// </summary>
        public IEnumerable<PaymentProvider> GetSupportedProviders()
        {
            var allProviders = Enum.GetValues<PaymentProvider>();
            var supportedProviders = allProviders.Where(IsProviderSupported).ToList();

            _logger.LogInformation("Found {Count} supported payment providers: {Providers}",
                supportedProviders.Count,
                string.Join(", ", supportedProviders));

            return supportedProviders;
        }

        /// <summary>
        /// Helper method to get a service from the service provider
        /// </summary>
        private T GetService<T>() where T : class
        {
            var service = _serviceProvider.GetService(typeof(T)) as T;
            if (service == null)
            {
                throw new InvalidOperationException($"Service {typeof(T).Name} is not registered in the DI container");
            }
            return service;
        }
    }
}
