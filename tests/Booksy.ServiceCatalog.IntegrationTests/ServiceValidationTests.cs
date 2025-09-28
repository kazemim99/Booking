// ========================================
// Example 1: Basic Test Using ServiceCatalogIntegrationTestBase
// ========================================
using Booksy.API;
using Booksy.ServiceCatalog.Api.Models.Requests;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Services;

// ========================================
// Example 6: Using Value Object Helpers
// ========================================
public class ServiceValidationTests : ServiceCatalogIntegrationTestBase
{
    public ServiceValidationTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateService_WithInvalidPrice_ShouldReturn400()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();

        // Use value object helper to create invalid price (will throw)
        Action act = () => CreatePrice(-10.00m); // Negative price

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    [InlineData(1000)]
    public async Task CreateService_WithInvalidDuration_ShouldReturn400(int invalidDuration)
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();

        var request = new CreateServiceRequest
        {
            ProviderId = provider.Id.Value,
            Name = "Invalid Duration Service",
            Description = "Testing invalid duration",
            CategoryName = "Beauty",
            ServiceType = ServiceType.Standard,
            BasePrice = 50.00m,
            Currency = "USD",
            DurationMinutes = invalidDuration
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
