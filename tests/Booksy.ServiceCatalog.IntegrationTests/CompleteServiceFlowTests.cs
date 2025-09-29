using Booksy.API;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Api.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.API.Models.Responses;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Services;

public class CompleteServiceFlowTests : ServiceCatalogIntegrationTestBase
{
    public CompleteServiceFlowTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CompleteServiceLifecycle_ShouldWorkEndToEnd()
    {
        // Step 1: Create provider and authenticate
        var provider = await CreateAndAuthenticateAsProviderAsync(
            businessName: "Full Service Salon",
            email: "fullservice@salon.com"
        );

        // Step 2: Create a service
        var createRequest = new CreateServiceRequest
        {
            ProviderId = provider.Id.Value,
            Name = "Complete Service",
            Description = "End-to-end test service",
            CategoryName = "Beauty & Wellness",
            ServiceType = ServiceType.Premium,
            BasePrice = 100.00m,
            Currency = "USD",
            DurationMinutes = 90
        };

        var createResponse = await PostAsJsonAsync("/api/v1/services", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdService = await GetResponseAsync<ServiceResponse>(createResponse);
        await AssertServiceExistsAsync(createdService!.Id);

        // Step 3: Update the service
        var updateRequest = new UpdateServiceRequest
        {
            Name = "Updated Complete Service",
            Description = "Updated description",
            CategoryName = "Beauty & Wellness",
            ServiceType = ServiceType.Standard,
            BasePrice = 90.00m,
            Currency = "USD",
            DurationMinutes = 90

        };

        var updateResponse = await PutAsJsonAsync(
            $"/api/v1/services/{createdService.Id}",
            updateRequest
        );
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 4: Verify update in database
        var updatedService = await FindServiceAsync(createdService.Id);
        updatedService!.Name.Should().Be(updatedService.Name);
        updatedService.BasePrice.Amount.Should().Be(updateRequest.BasePrice);

        // Step 5: Get service via API
        var getResponse = await GetServiceByIdAsync(createdService.Id);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 6: Activate service
        await AssertServiceStatusAsync(createdService.Id, ServiceStatus.Draft);

        var activateResponse = await PostAsJsonAsync(
            $"/api/v1/services/{createdService.Id}/activate",
            new { }
        );
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await AssertServiceStatusAsync(createdService.Id, ServiceStatus.Active);

        // Step 7: Search for the service
        var searchResponse = await SearchServicesAsync(
            searchTerm: "Complete",
            minPrice: 90.00m,
            maxPrice: 150.00m
        );
        var searchResults = await GetResponseAsync<PagedResult<ServiceSearchResponse>>(searchResponse);
        searchResults.Items.Should().Contain(s => s.Id == createdService.Id);

        // Step 8: Delete the service
        var deleteResponse = await DeleteServiceAsync(createdService.Id);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await AssertServiceNotExistsAsync(createdService.Id);
        await AssertProviderServiceCountAsync(provider.Id.Value, 0);
    }
}