using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Booksy.ServiceCatalog.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace Booksy.ServiceCatalog.IntegrationTests.StepDefinitions.Services;

[Binding]
public class ServiceSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ServiceCatalogIntegrationTestBase _testBase;
    private readonly ScenarioContextHelper _helper;

    public ServiceSteps(
        ScenarioContext scenarioContext,
        ServiceCatalogIntegrationTestBase testBase)
    {
        _scenarioContext = scenarioContext;
        _testBase = testBase;
        _helper = _scenarioContext.Get<ScenarioContextHelper>("Helper");
    }

    [When(@"I send a POST request to create a service with:")]
    public async Task WhenISendAPostRequestToCreateAServiceWith(Table table)
    {
        var provider = _scenarioContext.Get<Domain.Aggregates.Provider>("Provider:Current");
        var requestData = _helper.BuildDictionaryFromTable(table);

        var request = new
        {
            ServiceName = requestData["ServiceName"],
            Description = requestData["Description"],
            Duration = requestData["Duration"],
            DurationHours = 0,
            BasePrice = requestData["BasePrice"],
            Currency = requestData["Currency"],
            Category = requestData["Category"],
            IsMobileService = requestData.ContainsKey("IsMobileService")
                ? requestData["IsMobileService"]
                : false
        };

        var response = await _testBase.PostAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/services", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I send a DELETE request to remove the service")]
    public async Task WhenISendADeleteRequestToRemoveTheService()
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>("Service:Current");

        var response = await _testBase.DeleteAsync(
            $"/api/v1/services/{service.Id.Value}");

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [Then(@"the service should exist in the database")]
    public async Task ThenTheServiceShouldExistInTheDatabase()
    {
        var response = _scenarioContext.Get<Core.Domain.Infrastructure.Middleware.ApiResponse>("LastResponse");

        // Extract service ID from response data
        var serviceId = GetServiceIdFromResponse(response);

        var service = await _testBase.DbContext.Services
            .FirstOrDefaultAsync(s => s.Id == Domain.ValueObjects.ServiceId.From(serviceId));

        service.Should().NotBeNull($"Service should exist in database");
    }

    [Then(@"the service should be deleted from the database")]
    public async Task ThenTheServiceShouldBeDeletedFromTheDatabase()
    {
        var service = _scenarioContext.Get<Domain.Aggregates.Service>("Service:Current");

        var deletedService = await _testBase.DbContext.Services
            .FirstOrDefaultAsync(s => s.Id == service.Id);

        deletedService.Should().BeNull($"Service should be deleted from database");
    }

    [Then(@"the provider should have (.*) services")]
    public async Task ThenTheProviderShouldHaveServices(int expectedCount)
    {
        var provider = _scenarioContext.Get<Domain.Aggregates.Provider>("Provider:Current");

        var services = await _testBase.GetProviderServicesAsync(provider.Id.Value);
        services.Should().HaveCount(expectedCount);
    }

    private Guid GetServiceIdFromResponse(object response)
    {
        var dataProperty = response.GetType().GetProperty("Data");
        if (dataProperty == null)
            return Guid.Empty;

        var data = dataProperty.GetValue(response);
        if (data == null)
            return Guid.Empty;

        var idProperty = data.GetType().GetProperty("Id");
        if (idProperty == null)
            return Guid.Empty;

        return (Guid)idProperty.GetValue(data);
    }
}
