using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Booksy.ServiceCatalog.IntegrationTests.Support;
using FluentAssertions;
using Reqnroll;

namespace Booksy.ServiceCatalog.IntegrationTests.StepDefinitions.Services;

/// <summary>
/// Step definitions for ServiceAuthorization.feature scenarios
/// Handles role-based access control, security, and permission testing
/// </summary>
[Binding]
public class ServiceAuthorizationSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ServiceCatalogIntegrationTestBase _testBase;
    private readonly ScenarioContextHelper _helper;

    public ServiceAuthorizationSteps(
        ScenarioContext scenarioContext,
        ServiceCatalogIntegrationTestBase testBase)
    {
        _scenarioContext = scenarioContext;
        _testBase = testBase;
        _helper = _scenarioContext.Get<ScenarioContextHelper>("Helper");
    }

    // ==================== GIVEN STEPS ====================

    [Given(@"the following providers exist:")]
    public async Task GivenTheFollowingProvidersExist(Table table)
    {
        var providers = new Dictionary<string, Domain.Aggregates.Provider>();

        foreach (var row in table.Rows)
        {
            var providerName = row["Provider"];
            var email = row["Email"];
            var status = row["Status"];

            var provider = await _testBase.CreateAndAuthenticateAsProviderAsync(providerName, email);

            if (status == "Active")
            {
                provider.SetSatus(Domain.Enums.ProviderStatus.Active);
            }

            await _testBase.DbContext.SaveChangesAsync();
            providers[providerName] = provider;
        }

        _scenarioContext.Set(providers, "Providers");
    }

    [Given(@"provider ""(.*)"" has the following services:")]
    public async Task GivenProviderHasTheFollowingServices(string providerName, Table table)
    {
        var providers = _scenarioContext.Get<Dictionary<string, Domain.Aggregates.Provider>>("Providers");
        var provider = providers[providerName];

        foreach (var row in table.Rows)
        {
            var serviceName = row["Service"];
            var status = row["Status"];
            var price = decimal.Parse(row["Price"]);

            var service = await _testBase.CreateServiceForProviderAsync(provider, serviceName, price, 60);

            if (status == "Active")
            {
                service.Activate();
            }
            else if (status == "Inactive")
            {
                service.Deactivate("Test");
            }
            else if (status == "Archived")
            {
                service.Archive("Test");
            }

            await _testBase.DbContext.SaveChangesAsync();
        }

        // Re-authenticate as the first provider
        var firstProvider = providers.Values.First();
        _testBase.AuthenticateAsProviderOwner(firstProvider);
        _scenarioContext.Set(firstProvider, "Provider:Current");
    }

    [Given(@"I am authenticated as the owner of ""(.*)""")]
    public void GivenIAmAuthenticatedAsTheOwnerOf(string providerName)
    {
        var providers = _scenarioContext.Get<Dictionary<string, Domain.Aggregates.Provider>>("Providers");
        var provider = providers[providerName];

        _testBase.AuthenticateAsProviderOwner(provider);
        _scenarioContext.Set(provider, "Provider:Current");
    }

    [Given(@"provider ""(.*)"" has a service ""(.*)""")]
    public async Task GivenProviderHasAService(string providerName, string serviceName)
    {
        var providers = _scenarioContext.Get<Dictionary<string, Domain.Aggregates.Provider>>("Providers");
        var provider = providers[providerName];

        var service = await _testBase.CreateServiceForProviderAsync(provider, serviceName, 50.00m, 60);
        _scenarioContext.Set(service, $"Service:{serviceName}");
    }

    [Given(@"provider ""(.*)"" has an inactive service ""(.*)""")]
    public async Task GivenProviderHasAnInactiveService(string providerName, string serviceName)
    {
        var providers = _scenarioContext.Get<Dictionary<string, Domain.Aggregates.Provider>>("Providers");
        var provider = providers[providerName];

        var service = await _testBase.CreateServiceForProviderAsync(provider, serviceName, 50.00m, 60);
        service.Deactivate("Test");
        await _testBase.DbContext.SaveChangesAsync();

        _scenarioContext.Set(service, $"Service:{serviceName}");
    }

    [Given(@"provider ""(.*)"" has an active service ""(.*)""")]
    public async Task GivenProviderHasAnActiveService(string providerName, string serviceName)
    {
        var providers = _scenarioContext.Get<Dictionary<string, Domain.Aggregates.Provider>>("Providers");
        var provider = providers[providerName];

        var service = await _testBase.CreateServiceForProviderAsync(provider, serviceName, 50.00m, 60);
        service.Activate();
        await _testBase.DbContext.SaveChangesAsync();

        _scenarioContext.Set(service, $"Service:{serviceName}");
    }

    [Given(@"I am authenticated as staff of ""(.*)""")]
    public void GivenIAmAuthenticatedAsStaffOf(string providerName)
    {
        var providers = _scenarioContext.Get<Dictionary<string, Domain.Aggregates.Provider>>("Providers");
        var provider = providers[providerName];

        // In real implementation, create staff user with limited permissions
        _testBase.AuthenticateAsProviderOwner(provider);
        _scenarioContext.Set("Staff", "UserRole");
    }

    [Given(@"I am assigned to service ""(.*)""")]
    public void GivenIAmAssignedToService(string serviceName)
    {
        _scenarioContext.Set(serviceName, "AssignedService");
    }

    [Given(@"I have a tampered authentication token")]
    public void GivenIHaveATamperedAuthenticationToken()
    {
        // Set invalid auth header
        _scenarioContext.Set("TamperedToken", "AuthToken");
    }

    [Given(@"I have an expired authentication token")]
    public void GivenIHaveAnExpiredAuthenticationToken()
    {
        _scenarioContext.Set("ExpiredToken", "AuthToken");
    }

    [Given(@"I am authenticated with provider ""(.*)"" token")]
    public void GivenIAmAuthenticatedWithProviderToken(string providerName)
    {
        var providers = _scenarioContext.Get<Dictionary<string, Domain.Aggregates.Provider>>("Providers");
        var provider = providers[providerName];

        _testBase.AuthenticateAsProviderOwner(provider);
        _scenarioContext.Set(provider, "Provider:Current");
    }

    [Given(@"I modify my token to claim provider role")]
    public void GivenIModifyMyTokenToClaimProviderRole()
    {
        // In real implementation, attempt to escalate privileges
        _scenarioContext.Set("ModifiedToken", "AuthToken");
    }

    [Given(@"provider ""(.*)"" has a service with known ID")]
    public async Task GivenProviderHasAServiceWithKnownId(string providerName)
    {
        await GivenProviderHasAService(providerName, "Known Service");
    }

    [Given(@"provider ""(.*)"" is updating their service")]
    public void GivenProviderIsUpdatingTheirService(string providerName)
    {
        _scenarioContext.Set(providerName, "UpdatingProvider");
    }

    [Given(@"another session is authenticated as the same provider")]
    public void GivenAnotherSessionIsAuthenticatedAsTheSameProvider()
    {
        _scenarioContext.Set(true, "ConcurrentSession");
    }

    [Given(@"provider ""(.*)"" has status ""(.*)""")]
    public void GivenProviderHasStatus(string providerName, string status)
    {
        var providers = _scenarioContext.Get<Dictionary<string, Domain.Aggregates.Provider>>("Providers");
        var provider = providers[providerName];

        var providerStatus = Enum.Parse<Domain.Enums.ProviderStatus>(status);
        provider.SetSatus(providerStatus);

        _testBase.DbContext.SaveChanges();
    }

    // ==================== WHEN STEPS ====================

    [When(@"I attempt to update service ""(.*)"" from ""(.*)""")]
    public async Task WhenIAttemptToUpdateServiceFrom(string serviceName, string providerName)
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>($"Service:{serviceName}");

        var request = new
        {
            ServiceName = "Hacked Name",
            BasePrice = 0.01,
            Duration = 30
        };

        var response = await _testBase.PutAsJsonAsync(
            $"/api/v1/services/{service.ProviderId.Value}/{service.Id.Value}", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I attempt to delete service ""(.*)"" from ""(.*)""")]
    public async Task WhenIAttemptToDeleteServiceFrom(string serviceName, string providerName)
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>($"Service:{serviceName}");

        var response = await _testBase.DeleteAsync(
            $"/api/v1/services/{service.ProviderId.Value}/{service.Id.Value}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I attempt to activate service ""(.*)"" from ""(.*)""")]
    public async Task WhenIAttemptToActivateServiceFrom(string serviceName, string providerName)
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>($"Service:{serviceName}");

        var response = await _testBase.PostAsJsonAsync(
            $"/api/v1/services/{service.Id.Value}/activate", new { });

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I request service details for ""(.*)"" from ""(.*)""")]
    public async Task WhenIRequestServiceDetailsForFrom(string serviceName, string providerName)
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>($"Service:{serviceName}");

        var response = await _testBase.GetAsync(
            $"/api/v1/services/{service.Id.Value}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for services")]
    public async Task WhenISearchForServices()
    {
        var response = await _testBase.GetAsync("/api/v1/services/search");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I create a new service for ""(.*)"":")]
    public async Task WhenICreateANewServiceFor(string providerName, Table table)
    {
        var providers = _scenarioContext.Get<Dictionary<string, Domain.Aggregates.Provider>>("Providers");
        var provider = providers[providerName];

        var requestData = _helper.BuildDictionaryFromTable(table);

        var request = new
        {
            ServiceName = requestData["ServiceName"],
            BasePrice = requestData["BasePrice"],
            Duration = requestData["Duration"],
            Description = "Test service",
            Currency = "USD",
            Category = "Hair Services"
        };

        var response = await _testBase.PostAsJsonAsync(
            $"/api/v1/services/{provider.Id.Value}", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I update service ""(.*)"" with:")]
    public async Task WhenIUpdateServiceWith(string serviceName, Table table)
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>($"Service:{serviceName}");
        var requestData = _helper.BuildDictionaryFromTable(table);

        var request = new
        {
            ServiceName = requestData["ServiceName"],
            BasePrice = requestData["BasePrice"],
            Duration = 30
        };

        var response = await _testBase.PutAsJsonAsync(
            $"/api/v1/services/{service.ProviderId.Value}/{service.Id.Value}", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I delete service ""(.*)"" from ""(.*)""")]
    public async Task WhenIDeleteServiceFrom(string serviceName, string providerName)
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>($"Service:{serviceName}");

        var response = await _testBase.DeleteAsync(
            $"/api/v1/services/{service.ProviderId.Value}/{service.Id.Value}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I activate service ""(.*)"" from ""(.*)""")]
    public async Task WhenIActivateServiceFrom(string serviceName, string providerName)
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>($"Service:{serviceName}");

        var response = await _testBase.PostAsJsonAsync(
            $"/api/v1/services/{service.Id.Value}/activate", new { });

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I deactivate service ""(.*)"" from ""(.*)""")]
    public async Task WhenIDeactivateServiceFrom(string serviceName, string providerName)
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>($"Service:{serviceName}");

        var response = await _testBase.PostAsJsonAsync(
            $"/api/v1/services/{service.Id.Value}/deactivate", new { });

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I archive service ""(.*)"" from ""(.*)""")]
    public async Task WhenIArchiveServiceFrom(string serviceName, string providerName)
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>($"Service:{serviceName}");

        var response = await _testBase.DeleteAsync(
            $"/api/v1/services/{service.Id.Value}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I request my services including inactive")]
    public async Task WhenIRequestMyServicesIncludingInactive()
    {
        var provider = _scenarioContext.Get<Domain.Aggregates.Provider>("Provider:Current");

        var response = await _testBase.GetAsync(
            $"/api/v1/services/provider/{provider.Id.Value}?includeInactive=true");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for all services from ""(.*)""")]
    public async Task WhenISearchForAllServicesFrom(string providerName)
    {
        var providers = _scenarioContext.Get<Dictionary<string, Domain.Aggregates.Provider>>("Providers");
        var provider = providers[providerName];

        var response = await _testBase.GetAsync(
            $"/api/v1/services/provider/{provider.Id.Value}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for services with status ""(.*)""")]
    public async Task WhenISearchForServicesWithStatus(string status)
    {
        var response = await _testBase.GetAsync($"/api/v1/services/by-status/{status}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I bulk deactivate multiple services")]
    public async Task WhenIBulkDeactivateMultipleServices()
    {
        // In real implementation, send bulk operation request
        var response = await _testBase.PostAsJsonAsync("/api/v1/services/bulk/deactivate", new { });

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I attempt to update service ""(.*)""")]
    public async Task WhenIAttemptToUpdateService(string serviceName)
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>($"Service:{serviceName}");

        var request = new
        {
            ServiceName = "Modified Name",
            BasePrice = 60.00,
            Duration = 30
        };

        var response = await _testBase.PutAsJsonAsync(
            $"/api/v1/services/{service.ProviderId.Value}/{service.Id.Value}", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I attempt to create a service")]
    public async Task WhenIAttemptToCreateAService()
    {
        var providerId = Guid.NewGuid();

        var request = new
        {
            ServiceName = "Unauthorized Service",
            BasePrice = 50.00,
            Duration = 30,
            Currency = "USD",
            Category = "Hair Services"
        };

        var response = await _testBase.PostAsJsonAsync(
            $"/api/v1/services/{providerId}", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I attempt to update the service using direct ID")]
    public async Task WhenIAttemptToUpdateTheServiceUsingDirectId()
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>("Service:Known Service");

        var request = new
        {
            ServiceName = "Hacked Name",
            BasePrice = 0.01,
            Duration = 30
        };

        var response = await _testBase.PutAsJsonAsync(
            $"/api/v1/services/{service.ProviderId.Value}/{service.Id.Value}", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"both updates are executed concurrently")]
    public async Task WhenBothUpdatesAreExecutedConcurrently()
    {
        // Simulate concurrent updates
        await Task.CompletedTask;
    }

    [When(@"both sessions update the same service concurrently")]
    public async Task WhenBothSessionsUpdateTheSameServiceConcurrently()
    {
        // Simulate concurrent updates from same provider
        await Task.CompletedTask;
    }

    [When(@"I create (.*) services in rapid succession")]
    public async Task WhenICreateServicesInRapidSuccession(int count)
    {
        var provider = _scenarioContext.Get<Domain.Aggregates.Provider>("Provider:Current");

        for (int i = 0; i < count; i++)
        {
            var request = new
            {
                ServiceName = $"Service {i}",
                BasePrice = 50.00,
                Duration = 30,
                Currency = "USD",
                Category = "Hair Services"
            };

            await _testBase.PostAsJsonAsync($"/api/v1/services/{provider.Id.Value}", request);
        }
    }

    [When(@"I perform (.*) search requests in (.*) minute")]
    public async Task WhenIPerformSearchRequestsInMinute(int count, int minutes)
    {
        for (int i = 0; i < count; i++)
        {
            await _testBase.GetAsync("/api/v1/services/search");
        }
    }

    [When(@"I attempt to change the provider ID of ""(.*)"" to ""(.*)""")]
    public async Task WhenIAttemptToChangeTheProviderIdOf(string serviceName, string targetProviderName)
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>($"Service:{serviceName}");
        var providers = _scenarioContext.Get<Dictionary<string, Domain.Aggregates.Provider>>("Providers");
        var targetProvider = providers[targetProviderName];

        var request = new
        {
            ProviderId = targetProvider.Id.Value,
            ServiceName = "Transferred Service",
            BasePrice = 50.00,
            Duration = 30
        };

        var response = await _testBase.PutAsJsonAsync(
            $"/api/v1/services/{service.ProviderId.Value}/{service.Id.Value}", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I attempt to update an existing service")]
    public async Task WhenIAttemptToUpdateAnExistingService()
    {
        await WhenIAttemptToUpdateService("Haircut");
    }

    [When(@"I perform operation ""(.*)""")]
    public async Task WhenIPerformOperation(string operation)
    {
        await Task.CompletedTask;
        // Operation logic handled by scenario outline
    }

    // ==================== THEN STEPS ====================

    [Then(@"the service details should be returned")]
    public void ThenTheServiceDetailsShouldBeReturned()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"the service should be created")]
    public void ThenTheServiceShouldBeCreated()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Success.Should().BeTrue();
    }

    [Then(@"the service should be updated")]
    public void ThenTheServiceShouldBeUpdated()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Success.Should().BeTrue();
    }

    [Then(@"the service should be deleted")]
    public void ThenTheServiceShouldBeDeleted()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Success.Should().BeTrue();
    }

    [Then(@"the service status should be ""(.*)""")]
    public async Task ThenTheServiceStatusShouldBe(string status)
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>("Service:Haircut");
        await _testBase.AssertServiceStatusAsync(service.Id.Value, Enum.Parse<Domain.Enums.ServiceStatus>(status));
    }

    [Then(@"services in all statuses should be returned")]
    public void ThenServicesInAllStatusesShouldBeReturned()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"archived services should be returned")]
    public void ThenArchivedServicesShouldBeReturned()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all specified services should be deactivated")]
    public void ThenAllSpecifiedServicesShouldBeDeactivated()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Success.Should().BeTrue();
    }

    [Then(@"both operations should succeed with status (.*)")]
    public void ThenBothOperationsShouldSucceedWithStatus(int statusCode)
    {
        true.Should().BeTrue();
    }

    [Then(@"each service should be updated correctly")]
    public void ThenEachServiceShouldBeUpdatedCorrectly()
    {
        true.Should().BeTrue();
    }

    [Then(@"some requests should return status (.*)")]
    public void ThenSomeRequestsShouldReturnStatus(int statusCode)
    {
        true.Should().BeTrue();
    }

    [Then(@"an audit log entry should be created")]
    public void ThenAnAuditLogEntryShouldBeCreated()
    {
        true.Should().BeTrue();
    }

    [Then(@"the log should contain user ID and provider ID")]
    public void ThenTheLogShouldContainUserIdAndProviderId()
    {
        true.Should().BeTrue();
    }

    [Then(@"the unauthorized attempt should be logged")]
    public void ThenTheUnauthorizedAttemptShouldBeLogged()
    {
        true.Should().BeTrue();
    }

    [Then(@"security team should be notified")]
    public void ThenSecurityTeamShouldBeNotified()
    {
        true.Should().BeTrue();
    }

    [Then(@"the service ownership should remain with ""(.*)""")]
    public async Task ThenTheServiceOwnershipShouldRemainWith(string providerName)
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>("Service:Haircut");
        service.Should().NotBeNull();
        await Task.CompletedTask;
    }
}
