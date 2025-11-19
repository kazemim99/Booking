// ========================================
// Booksy.ServiceCatalog.IntegrationTests/Infrastructure/ServiceCatalogTestWebApplicationFactory.cs
// ========================================
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.Tests.Commons;
using Microsoft.Extensions.DependencyInjection;

namespace Booksy.ServiceCatalog.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for Service Catalog integration tests
/// Inherits from generic TestWebApplicationFactory and can add Service Catalog-specific configuration
/// </summary>
public class ServiceCatalogTestWebApplicationFactory<TStartup>
    : TestWebApplicationFactory<TStartup, ServiceCatalogDbContext>
    where TStartup : class
{
    public ServiceCatalogTestWebApplicationFactory()
        : base("ServiceCatalog")
    {
    }

    /// <summary>
    /// Configure Service Catalog-specific test services
    /// </summary>
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        // Add Service Catalog-specific test service replacements here
        // Example: Replace external payment gateway with fake
        // services.RemoveAll<IPaymentGateway>();
        // services.AddSingleton<IPaymentGateway, FakePaymentGateway>();

        // Example: Replace SMS service with fake
        // services.RemoveAll<ISmsService>();
        // services.AddSingleton<ISmsService, FakeSmsService>();
    }
}
