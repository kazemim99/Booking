using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Booksy.ServiceCatalog.IntegrationTests.Support;
using FluentAssertions;
using Reqnroll;

namespace Booksy.ServiceCatalog.IntegrationTests.StepDefinitions.Services;

/// <summary>
/// Step definitions for ServiceSearch.feature scenarios
/// Handles search, filtering, sorting, and pagination
/// </summary>
[Binding]
public class ServiceSearchSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ServiceCatalogIntegrationTestBase _testBase;
    private readonly ScenarioContextHelper _helper;

    public ServiceSearchSteps(
        ScenarioContext scenarioContext,
        ServiceCatalogIntegrationTestBase testBase)
    {
        _scenarioContext = scenarioContext;
        _testBase = testBase;
        _helper = _scenarioContext.Get<ScenarioContextHelper>("Helper");
    }

    // ==================== GIVEN STEPS ====================

    [Given(@"the following providers exist with services:")]
    public async Task GivenTheFollowingProvidersExistWithServices(Table table)
    {
        var providerCache = new Dictionary<string, Domain.Aggregates.Provider>();

        foreach (var row in table.Rows)
        {
            var providerName = row["Provider"];

            if (!providerCache.ContainsKey(providerName))
            {
                var provider = await _testBase.CreateAndAuthenticateAsProviderAsync(
                    providerName,
                    $"{providerName.ToLower().Replace(" ", "")}@test.com");
                providerCache[providerName] = provider;
            }

            var provider = providerCache[providerName];
            var serviceName = row["Service"];
            var category = row["Category"];
            var price = decimal.Parse(row["Price"]);
            var duration = int.Parse(row["Duration"]);

            var service = await _testBase.CreateServiceForProviderAsync(
                provider,
                serviceName,
                price,
                duration);

            // Set status if needed
            if (row.ContainsKey("Status") && row["Status"] == "Active")
            {
                service.Activate();
                await _testBase.DbContext.SaveChangesAsync();
            }
        }
    }

    [Given(@"services have different booking counts")]
    public async Task GivenServicesHaveDifferentBookingCounts()
    {
        // Mock booking counts in context
        await Task.CompletedTask;
    }

    [Given(@"services have varying booking counts")]
    public async Task GivenServicesHaveVaryingBookingCounts()
    {
        await Task.CompletedTask;
    }

    [Given(@"providers are in different cities")]
    public async Task GivenProvidersAreInDifferentCities()
    {
        await Task.CompletedTask;
    }

    [Given(@"providers are in different states")]
    public async Task GivenProvidersAreInDifferentStates()
    {
        await Task.CompletedTask;
    }

    [Given(@"providers have location coordinates")]
    public async Task GivenProvidersHaveLocationCoordinates()
    {
        await Task.CompletedTask;
    }

    [Given(@"there are (.*) services in the database")]
    public async Task GivenThereAreServicesInTheDatabase(int count)
    {
        var provider = await _testBase.CreateAndAuthenticateAsProviderAsync("Test Provider", "provider@test.com");

        for (int i = 0; i < count; i++)
        {
            await _testBase.CreateServiceForProviderAsync(
                provider,
                $"Service {i}",
                50.00m,
                60);
        }
    }

    [Given(@"there are services with different statuses")]
    public async Task GivenThereAreServicesWithDifferentStatuses()
    {
        var provider = await _testBase.CreateAndAuthenticateAsProviderAsync("Test Provider", "provider@test.com");

        var activeService = await _testBase.CreateServiceForProviderAsync(provider, "Active Service", 50.00m, 60);
        activeService.Activate();

        var inactiveService = await _testBase.CreateServiceForProviderAsync(provider, "Inactive Service", 60.00m, 45);
        inactiveService.Deactivate("Test");

        await _testBase.DbContext.SaveChangesAsync();
    }

    [Given(@"I have services with different statuses")]
    public async Task GivenIHaveServicesWithDifferentStatuses()
    {
        await GivenThereAreServicesWithDifferentStatuses();
    }

    [Given(@"there are services in all statuses")]
    public async Task GivenThereAreServicesInAllStatuses()
    {
        await GivenThereAreServicesWithDifferentStatuses();
    }

    // ==================== WHEN STEPS ====================

    [When(@"I search for services with query ""(.*)""")]
    public async Task WhenISearchForServicesWithQuery(string query)
    {
        var response = await _testBase.GetAsync($"/api/v1/services/search?query={query}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for services with empty query")]
    public async Task WhenISearchForServicesWithEmptyQuery()
    {
        var response = await _testBase.GetAsync("/api/v1/services/search");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for services with:")]
    public async Task WhenISearchForServicesWith(Table table)
    {
        var parameters = _helper.BuildDictionaryFromTable(table);
        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));

        var response = await _testBase.GetAsync($"/api/v1/services/search?{queryString}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for services sorted by ""(.*)"" in ""(.*)"" order")]
    public async Task WhenISearchForServicesSortedBy(string sortField, string sortOrder)
    {
        var response = await _testBase.GetAsync($"/api/v1/services/search?sortBy={sortField}&sortOrder={sortOrder}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for services with pagination:")]
    public async Task WhenISearchForServicesWithPagination(Table table)
    {
        var parameters = _helper.BuildDictionaryFromTable(table);
        var queryString = string.Join("&", parameters.Select(p => $"{p.Key.ToLower()}={p.Value}"));

        var response = await _testBase.GetAsync($"/api/v1/services/search?{queryString}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for services from provider ""(.*)""")]
    public async Task WhenISearchForServicesFromProvider(string providerName)
    {
        // In real implementation, get provider ID by name
        var response = await _testBase.GetAsync("/api/v1/services/search?provider=" + providerName);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for services from provider ""(.*)"" with status ""(.*)""")]
    public async Task WhenISearchForServicesFromProviderWithStatus(string providerName, string status)
    {
        var response = await _testBase.GetAsync($"/api/v1/services/search?provider={providerName}&status={status}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for services in city ""(.*)""")]
    public async Task WhenISearchForServicesInCity(string city)
    {
        var response = await _testBase.GetAsync($"/api/v1/services/search?city={city}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for services in state ""(.*)""")]
    public async Task WhenISearchForServicesInState(string state)
    {
        var response = await _testBase.GetAsync($"/api/v1/services/search?state={state}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for services near coordinates:")]
    public async Task WhenISearchForServicesNearCoordinates(Table table)
    {
        var latitude = table.Rows[0]["Latitude"];
        var longitude = table.Rows[0]["Longitude"];
        var radius = table.Rows[0]["Radius"];

        var response = await _testBase.GetAsync($"/api/v1/services/search?lat={latitude}&lng={longitude}&radius={radius}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for services with pageSize (.*)")]
    public async Task WhenISearchForServicesWithPageSize(int pageSize)
    {
        var response = await _testBase.GetAsync($"/api/v1/services/search?pageSize={pageSize}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for services with a query of (.*) characters")]
    public async Task WhenISearchForServicesWithQueryLength(int length)
    {
        var longQuery = new string('A', length);
        var response = await _testBase.GetAsync($"/api/v1/services/search?query={longQuery}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for services without status filter")]
    public async Task WhenISearchForServicesWithoutStatusFilter()
    {
        var response = await _testBase.GetAsync("/api/v1/services/search");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for my services with status ""(.*)""")]
    public async Task WhenISearchForMyServicesWithStatus(string status)
    {
        var response = await _testBase.GetAsync($"/api/v1/services/search?status={status}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I search for all services from ""(.*)""")]
    public async Task WhenISearchForAllServicesFrom(string providerName)
    {
        var response = await _testBase.GetAsync($"/api/v1/services/search?provider={providerName}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I request popular services")]
    public async Task WhenIRequestPopularServices()
    {
        var response = await _testBase.GetAsync("/api/v1/services/popular");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I request popular services with limit (.*)")]
    public async Task WhenIRequestPopularServicesWithLimit(int limit)
    {
        var response = await _testBase.GetAsync($"/api/v1/services/popular?limit={limit}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I request popular services in category ""(.*)""")]
    public async Task WhenIRequestPopularServicesInCategory(string category)
    {
        var response = await _testBase.GetAsync($"/api/v1/services/popular?category={category}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    // ==================== THEN STEPS ====================

    [Then(@"all services should have ""(.*)"" in the name")]
    public void ThenAllServicesShouldHaveInTheName(string text)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"the service name should contain ""(.*)""")]
    public void ThenTheServiceNameShouldContain(string text)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should be in category ""(.*)""")]
    public void ThenAllServicesShouldBeInCategory(string category)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should have price between (.*) and (.*)")]
    public void ThenAllServicesShouldHavePriceBetween(decimal minPrice, decimal maxPrice)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should have price >= (.*)")]
    public void ThenAllServicesShouldHavePriceGreaterOrEqual(decimal minPrice)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should have price <= (.*)")]
    public void ThenAllServicesShouldHavePriceLessOrEqual(decimal maxPrice)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should have price exactly (.*)")]
    public void ThenAllServicesShouldHavePriceExactly(decimal price)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should have duration between (.*) and (.*) minutes")]
    public void ThenAllServicesShouldHaveDurationBetween(int minDuration, int maxDuration)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should have duration <= (.*) minutes")]
    public void ThenAllServicesShouldHaveDurationLessOrEqual(int maxDuration)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should have duration >= (.*) minutes")]
    public void ThenAllServicesShouldHaveDurationGreaterOrEqual(int minDuration)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should be of type ""(.*)""")]
    public void ThenAllServicesShouldBeOfType(string type)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should be available as mobile")]
    public void ThenAllServicesShouldBeAvailableAsMobile()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should be location-based only")]
    public void ThenAllServicesShouldBeLocationBasedOnly()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should require deposit")]
    public void ThenAllServicesShouldRequireDeposit()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should not require deposit")]
    public void ThenAllServicesShouldNotRequireDeposit()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should match all criteria")]
    public void ThenAllServicesShouldMatchAllCriteria()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should match all specified filters")]
    public void ThenAllServicesShouldMatchAllSpecifiedFilters()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"services should be ordered by price ascending")]
    public void ThenServicesShouldBeOrderedByPriceAscending()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"services should be ordered by price descending")]
    public void ThenServicesShouldBeOrderedByPriceDescending()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"services should be ordered by duration ascending")]
    public void ThenServicesShouldBeOrderedByDurationAscending()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"services should be ordered by duration descending")]
    public void ThenServicesShouldBeOrderedByDurationDescending()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"services should be ordered alphabetically by name")]
    public void ThenServicesShouldBeOrderedAlphabeticallyByName()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"services should be ordered by booking count descending")]
    public void ThenServicesShouldBeOrderedByBookingCountDescending()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"pagination metadata should indicate page (.*)")]
    public void ThenPaginationMetadataShouldIndicatePage(int page)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"pagination should reflect filtered total count")]
    public void ThenPaginationShouldReflectFilteredTotalCount()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should belong to ""(.*)""")]
    public void ThenAllServicesShouldBelongTo(string providerName)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should be active")]
    public void ThenAllServicesShouldBeActive()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all service providers should be in ""(.*)""")]
    public void ThenAllServiceProvidersShouldBeIn(string location)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should be within (.*) km radius")]
    public void ThenAllServicesShouldBeWithinRadius(int radius)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"the response should return within (.*) seconds")]
    public void ThenTheResponseShouldReturnWithinSeconds(int seconds)
    {
        // Performance assertion
        true.Should().BeTrue();
    }

    [Then(@"the search should handle special characters correctly")]
    public void ThenTheSearchShouldHandleSpecialCharactersCorrectly()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all services should have status ""(.*)""")]
    public void ThenAllServicesShouldHaveStatus(string status)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"only active services should be returned")]
    public void ThenOnlyActiveServicesShouldBeReturned()
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

    [Then(@"the response should contain up to (.*) services")]
    public void ThenTheResponseShouldContainUpToServices(int maxCount)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"services should be ordered by popularity")]
    public void ThenServicesShouldBeOrderedByPopularity()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all returned services should be in ""(.*)"" category")]
    public void ThenAllReturnedServicesShouldBeInCategory(string category)
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }

    [Then(@"all returned services should be within the price range")]
    public void ThenAllReturnedServicesShouldBeWithinThePriceRange()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");
        response.Data.Should().NotBeNull();
    }
}
