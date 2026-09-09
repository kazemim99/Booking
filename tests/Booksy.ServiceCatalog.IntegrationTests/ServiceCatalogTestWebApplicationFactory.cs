// ========================================
// Booksy.ServiceCatalog.IntegrationTests/Infrastructure/ServiceCatalogTestWebApplicationFactory.cs
// ========================================
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.Tests.Commons;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Booksy.Infrastructure.External.Payment;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.Core.Application.Services.Notifications;

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

        // The real registration resolves IPaymentGateway through the gateway factory to the
        // configured provider (ZarinPal by default), whose placeholder merchant id makes every
        // payment command fail with the gateway's own error (FOLLOW-UPS #31). Tests exercise
        // the application's payment behaviour, not the gateway's.
        services.RemoveAll<IPaymentGateway>();
        services.AddSingleton<IPaymentGateway, FakePaymentGateway>();

        // Every notification channel talks to the outside world (SMTP, SMS gateway, Firebase,
        // SignalR) and none of them can reach it from the test sandbox. The dispatcher rightly
        // records that as a failed delivery, so without these fakes every notification the API
        // sends reports success:false and no lifecycle assertion can say anything about the
        // application's own behaviour.
        services.RemoveAll<IEmailNotificationService>();
        services.AddSingleton<IEmailNotificationService, FakeEmailNotificationService>();
        services.RemoveAll<ISmsNotificationService>();
        services.AddSingleton<ISmsNotificationService, FakeSmsGateway>();
        services.RemoveAll<IPushNotificationService>();
        services.AddSingleton<IPushNotificationService, FakePushNotificationService>();
        services.RemoveAll<IInAppNotificationService>();
        services.AddSingleton<IInAppNotificationService, FakeInAppNotificationService>();
    }
}
