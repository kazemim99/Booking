using Booksy.ServiceCatalog.Application.Commands.Provider.Registration;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests;

/// <summary>
/// Integration tests for step-based progressive provider registration
/// Tests each registration step independently with proper validation
/// </summary>
[Collection("Integration Tests")]
public class StepBasedRegistrationTests : ServiceCatalogIntegrationTestBase
{
    public StepBasedRegistrationTests(ServiceCatalogTestWebApplicationFactory<Booksy.API.Startup> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Step3_CreateLocationDraft_WithValidData_CreatesNewDraft()
    {
        // Arrange
        AuthenticateAsCustomer("provider1@test.com");

        var request = new
        {
            BusinessName = "Progressive Salon",
            BusinessDescription = "A modern beauty salon",
            Category = "Salon",
            PhoneNumber = "+989123456789",
            Email = "contact@progressivesalon.com",
            AddressLine1 = "123 Test Street",
            AddressLine2 = "Suite 100",
            City = "Tehran",
            Province = "Tehran",
            PostalCode = "1234567890",
            Latitude = 35.6892m,
            Longitude = 51.3890m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/registration/step-3/location", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var data = JObject.Parse(content)["data"];

        data.Should().NotBeNull();
        data!["providerId"].Should().NotBeNull();
        data["registrationStep"]!.Value<int>().Should().Be(3);
        data["message"]!.ToString().Should().Contain("created");
    }

    [Fact]
    public async Task Step3_CreateLocationDraft_WhenAlreadyExists_ReturnsExistingDraft()
    {
        // Arrange
        AuthenticateAsCustomer("provider2@test.com");

        var request = new
        {
            BusinessName = "Test Salon",
            BusinessDescription = "Test Description",
            Category = "Salon",
            PhoneNumber = "+989123456789",
            Email = "test@test.com",
            AddressLine1 = "123 Street",
            City = "Tehran",
            Province = "Tehran",
            PostalCode = "1234567890",
            Latitude = 35.6892m,
            Longitude = 51.3890m
        };

        // Create first draft
        await Client.PostAsJsonAsync("/api/v1/registration/step-3/location", request);

        // Act - Try to create again
        var response = await Client.PostAsJsonAsync("/api/v1/registration/step-3/location", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var data = JObject.Parse(content)["data"];

        data!["message"]!.ToString().Should().Contain("updated");
    }

    [Fact]
    public async Task Step4_SaveServices_WithValidData_SavesSuccessfully()
    {
        // Arrange
        var providerId = await CreateDraftProviderAsync("provider3@test.com");

        var request = new
        {
            ProviderId = providerId,
            Services = new[]
            {
                new
                {
                    Name = "Haircut",
                    DurationHours = 1,
                    DurationMinutes = 0,
                    Price = 50.00m,
                    PriceType = "fixed"
                },
                new
                {
                    Name = "Hair Coloring",
                    DurationHours = 2,
                    DurationMinutes = 30,
                    Price = 150.00m,
                    PriceType = "variable"
                }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/registration/step-4/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var data = JObject.Parse(content)["data"];

        data!["servicesCount"]!.Value<int>().Should().Be(2);
        data["registrationStep"]!.Value<int>().Should().Be(4);
    }

    [Fact]
    public async Task Step5_SaveStaff_WithValidData_SavesSuccessfully()
    {
        // Arrange
        var providerId = await CreateDraftProviderAsync("provider4@test.com");

        var request = new
        {
            ProviderId = providerId,
            StaffMembers = new[]
            {
                new
                {
                    Name = "John Doe",
                    Email = "john@salon.com",
                    PhoneNumber = "+989121234567",
                    Position = "stylist"
                },
                new
                {
                    Name = "Jane Smith",
                    Email = "jane@salon.com",
                    PhoneNumber = "+989127654321",
                    Position = "receptionist"
                }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/registration/step-5/staff", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var data = JObject.Parse(content)["data"];

        data!["staffCount"]!.Value<int>().Should().Be(2);
        data["registrationStep"]!.Value<int>().Should().Be(5);
    }

    [Fact]
    public async Task Step6_SaveWorkingHours_WithValidData_SavesSuccessfully()
    {
        // Arrange
        var providerId = await CreateDraftProviderAsync("provider5@test.com");

        var request = new
        {
            ProviderId = providerId,
            BusinessHours = new[]
            {
                new
                {
                    DayOfWeek = 1, // Monday
                    IsOpen = true,
                    OpenTime = new { Hours = 9, Minutes = 0 },
                    CloseTime = new { Hours = 18, Minutes = 0 },
                    Breaks = new object[]
                    {
                        new
                        {
                            Start = new { Hours = 12, Minutes = 0 },
                            End = new { Hours = 13, Minutes = 0 }
                        }
                    }
                },
                new
                {
                    DayOfWeek = 0, // Sunday
                    IsOpen = false,
                    OpenTime = new { Hours = 0, Minutes = 0 },
                    CloseTime = new { Hours = 0, Minutes = 0 },
                    Breaks = new object[] { }
                }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/registration/step-6/working-hours", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var data = JObject.Parse(content)["data"];

        data!["openDaysCount"]!.Value<int>().Should().BeGreaterThan(0);
        data["registrationStep"]!.Value<int>().Should().Be(6);
    }

    [Fact]
    public async Task Step7_SaveGallery_MarksStepComplete()
    {
        // Arrange
        var providerId = await CreateDraftProviderAsync("provider6@test.com");

        var request = new
        {
            ProviderId = providerId,
            ImageUrls = new[] { "image1.jpg", "image2.jpg", "image3.jpg" }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/registration/step-7/gallery", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var data = JObject.Parse(content)["data"];

        data!["registrationStep"]!.Value<int>().Should().Be(7);
    }

    [Fact]
    public async Task Step8_SaveFeedback_WithOptionalFeedback_SavesSuccessfully()
    {
        // Arrange
        var providerId = await CreateDraftProviderAsync("provider7@test.com");

        var request = new
        {
            ProviderId = providerId,
            FeedbackText = "Great registration process!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/registration/step-8/feedback", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var data = JObject.Parse(content)["data"];

        data!["registrationStep"]!.Value<int>().Should().Be(8);
    }

    [Fact]
    public async Task Step9_CompleteRegistration_WithAllRequiredData_CompletesSuccessfully()
    {
        // Arrange
        var request = await CreateFullDraftProviderAsync("provider8@test.com");


        // Act
        var response = await PostAsJsonAsync<SaveStep9CompleteCommand, SaveStep9CompleteResult>("/api/v1/registration/step-9/complete", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);


        response.Data.Status.ToString().Should().Be("PendingVerification");
        response.Data.Message!.ToString().Should().Contain("completed");
    }

    [Fact]
    public async Task Step9_CompleteRegistration_WithoutServices_FailsValidation()
    {
        // Arrange
        var providerId = await CreateDraftProviderAsync("provider9@test.com");
        // Don't add services


        // Act
        var response = await PostAsJsonAsync<SaveStep9CompleteCommand, SaveStep9CompleteResult>("/api/v1/registration/step-9/complete", new SaveStep9CompleteCommand(providerId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProgress_WithExistingDraft_ReturnsDraftData()
    {
        // Arrange
        await CreateDraftProviderAsync("provider10@test.com");

        // Act
        var response = await Client.GetAsync("/api/v1/registration/progress");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var data = JObject.Parse(content)["data"];

        data!["hasDraft"]!.Value<bool>().Should().BeTrue();
        data["draftData"].Should().NotBeNull();
    }

    [Fact]
    public async Task GetProgress_WithoutDraft_ReturnsNotFound()
    {
        // Arrange
        AuthenticateAsCustomer("newuser@test.com");

        // Act
        var response = await Client.GetAsync("/api/v1/registration/progress");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Helper methods

    private async Task<Guid> CreateDraftProviderAsync(string email)
    {
        AuthenticateAsCustomer(email);

        var request = new SaveStep3LocationCommand(
        OwnerFirstName: "Mostafa",
        OwnerLastName: "Kazemi",
            BusinessName: "Test Business",
            BusinessDescription: "Test Description",
            Category: "Salon",
            PhoneNumber: "+989123456789",
            Email: email,
            AddressLine1: "123 Test St",
            AddressLine2: "",
            City: "Tehran",
            Province: "Tehran",
            PostalCode: "1234567890",
            Latitude: 35.6892m,
            Longitude: 51.3890m
        );

        var response = await PostAsJsonAsync<SaveStep3LocationCommand, SaveStep3LocationResult>("/api/v1/registration/step-3/location", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return response.Data.ProviderId;
    }

    private async Task<SaveStep9CompleteCommand> CreateFullDraftProviderAsync(string email)
    {
        var providerId = await CreateDraftProviderAsync(email);

        // Add services
        await Client.PostAsJsonAsync("/api/v1/registration/step-4/services", new
        {
            ProviderId = providerId,
            Services = new[]
            {
                new
                {
                    Name = "Test Service",
                    DurationHours = 1,
                    DurationMinutes = 0,
                    Price = 100m,
                    PriceType = "fixed"
                }
            }
        });

        // Add working hours
        await Client.PostAsJsonAsync("/api/v1/registration/step-6/working-hours", new
        {
            ProviderId = providerId,
            BusinessHours = new[]
            {
                new
                {
                    DayOfWeek = 1,
                    IsOpen = true,
                    OpenTime = new { Hours = 9, Minutes = 0 },
                    CloseTime = new { Hours = 17, Minutes = 0 },
                    Breaks = Array.Empty<object>()
                }
            }
        });

        return new SaveStep9CompleteCommand(providerId);
    }
}
