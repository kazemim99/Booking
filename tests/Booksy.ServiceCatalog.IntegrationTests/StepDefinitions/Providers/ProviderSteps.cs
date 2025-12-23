using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Booksy.ServiceCatalog.IntegrationTests.Support;
using FluentAssertions;
using Reqnroll;
using System.Net;

namespace Booksy.ServiceCatalog.IntegrationTests.StepDefinitions.Providers;

[Binding]
public class ProviderSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ServiceCatalogIntegrationTestBase _testBase;
    private readonly ScenarioContextHelper _helper;

    public ProviderSteps(
        ScenarioContext scenarioContext,
        ServiceCatalogIntegrationTestBase testBase)
    {
        _scenarioContext = scenarioContext;
        _testBase = testBase;
        _helper = _scenarioContext.Get<ScenarioContextHelper>("Helper");
    }

    [When(@"I send a POST request to register a provider with:")]
    public async Task WhenISendAPostRequestToRegisterAProviderWith(Table table)
    {
        var requestData = _helper.BuildDictionaryFromTable(table);

        var request = new RegisterProviderRequest
        {
            OwnerId = _scenarioContext.Get<Guid>("CurrentUserId"),
            BusinessName = (string)requestData["BusinessName"],
            Description = (string)requestData["Description"],
            PrimaryCategory = Enum.Parse<ServiceCategory>((string)requestData["Type"]),
            ContactInfo = new ContactInfoRequest
            {
                PrimaryPhone = (string)requestData["PrimaryPhone"]
            },
            Address = new AddressRequest
            {
                Street = (string)requestData["Street"],
                City = (string)requestData["City"],
                State = (string)requestData["State"],
                PostalCode = (string)requestData["PostalCode"],
                Country = (string)requestData["Country"]
            }
        };

        var response = await _testBase.PostAsJsonAsync<RegisterProviderRequest, ProviderDetailsResponse>(
            "/api/v1/providers/register", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");

        if (response.Data != null)
        {
            _scenarioContext.Set(response.Data, "LastProviderResponse");
            _scenarioContext.Set(response.Data.Id, "LastProviderId");
        }
    }

    [When(@"I send a PUT request to update provider profile with:")]
    public async Task WhenISendAPutRequestToUpdateProviderProfileWith(Table table)
    {
        var providerId = _scenarioContext.Get<Guid>("CurrentProviderId");
        var requestData = _helper.BuildDictionaryFromTable(table);

        var request = new
        {
            FullName = requestData["FullName"],
            Email = requestData["Email"],
            ProfileImageUrl = requestData["ProfileImageUrl"]
        };

        var response = await _testBase.PutAsJsonAsync(
            "/api/v1/providers/profile", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I send a PUT request to update business info with:")]
    public async Task WhenISendAPutRequestToUpdateBusinessInfoWith(Table table)
    {
        var requestData = _helper.BuildDictionaryFromTable(table);

        var request = new
        {
            BusinessName = requestData["BusinessName"],
            Description = requestData.ContainsKey("Description") ? requestData["Description"] : null,
            PhoneNumber = requestData["PhoneNumber"],
            Website = requestData.ContainsKey("Website") ? requestData["Website"] : null
        };

        var response = await _testBase.PutAsJsonAsync(
            "/api/v1/providers/business", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I upload a valid (profile image|business logo)")]
    public async Task WhenIUploadAValidImage(string imageType)
    {
        // Create test image
        var imageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "image", "test.png");

        var endpoint = imageType == "profile image"
            ? "/api/v1/providers/profile/image"
            : "/api/v1/providers/business/logo";

        var response = await _testBase.Client.PostAsync(endpoint, content);

        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
        _scenarioContext.Set(response, "LastHttpResponse");
    }

    [When(@"I send a GET request to ""(.*)""")]
    public async Task WhenISendAGetRequestTo(string endpoint)
    {
        endpoint = _helper.ReplaceContextPlaceholders(endpoint);
        var response = await _testBase.GetAsync(endpoint);

        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
        _scenarioContext.Set(response, "LastHttpResponse");
    }

    [When(@"I send a POST request to activate the provider")]
    public async Task WhenISendAPostRequestToActivateTheProvider()
    {
        var providerId = _scenarioContext.Get<Guid>("CurrentProviderId");

        var response = await _testBase.PostAsJsonAsync(
            $"/api/v1/providers/{providerId}/activate", new { });

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [Then(@"the response should contain a provider with:")]
    public void ThenTheResponseShouldContainAProviderWith(Table table)
    {
        var response = _scenarioContext.Get<ApiResponse<ProviderDetailsResponse>>("LastResponse");

        response.Data.Should().NotBeNull("Response should contain provider data");

        foreach (var row in table.Rows)
        {
            var field = row["Field"];
            var expectedValue = _helper.ReplaceContextPlaceholders(row["Value"]);

            var actualValue = GetFieldValue(response.Data, field);
            actualValue?.ToString().Should().Be(expectedValue,
                $"Field '{field}' should have value '{expectedValue}'");
        }
    }

    [Then(@"the provider should exist in the database")]
    public async Task ThenTheProviderShouldExistInTheDatabase()
    {
        var providerId = _scenarioContext.Get<Guid>("LastProviderId");

        var provider = await _testBase.FindProviderAsync(providerId);
        provider.Should().NotBeNull($"Provider with ID {providerId} should exist in database");
    }

    [Then(@"the provider status should be ""(.*)"" in the database")]
    public async Task ThenTheProviderStatusShouldBeInTheDatabase(string expectedStatus)
    {
        var providerId = _scenarioContext.Get<Guid>("CurrentProviderId");

        var provider = await _testBase.FindProviderAsync(providerId);
        provider.Should().NotBeNull();
        provider!.Status.ToString().Should().Be(expectedStatus);
    }

    [Then(@"the response should contain the image URL")]
    public async Task ThenTheResponseShouldContainTheImageUrl()
    {
        var response = _scenarioContext.Get<HttpResponseMessage>("LastHttpResponse");
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("ImageUrl");
        content.Should().Contain("/uploads/providers/");
    }

    private object? GetFieldValue(object obj, string fieldName)
    {
        if (obj == null)
            return null;

        var property = obj.GetType().GetProperty(fieldName);
        if (property == null)
            return null;

        return property.GetValue(obj);
    }
}
