using Booksy.ServiceCatalog.Api;

namespace Booksy.ServiceCatalog.IntegrationTests.Infrastructure;

/// <summary>
/// Concrete implementation of ServiceCatalogIntegrationTestBase for ReqnRoll scenarios
/// </summary>
public class ServiceCatalogReqnrollTestBase : ServiceCatalogIntegrationTestBase
{
    public ServiceCatalogReqnrollTestBase(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }
}
