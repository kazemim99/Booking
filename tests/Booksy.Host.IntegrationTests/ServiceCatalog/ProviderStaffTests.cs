using Booksy.API;
using Booksy.ServiceCatalog.API.Models.Responses;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;
using Xunit;
using static Booksy.ServiceCatalog.API.Controllers.V1.ProvidersController;

namespace Booksy.ServiceCatalog.IntegrationTests.API.ProviderStaff;

/// <summary>
/// Integration tests for Provider Staff Management endpoints
/// Covers: CRUD operations for provider staff members
/// Endpoints: GET/POST/PUT/DELETE /api/v1/providers/{id}/staff
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class ProviderStaffTests : ServiceCatalogIntegrationTestBase
{
    public ProviderStaffTests(BooksyHostFactory factory) : base(factory)
    {
    }
    #region Get Staff Tests

    [Fact]
    public async Task GetProviderStaff_AsOwner_ShouldReturn200WithStaffList()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Staff Salon", "staff@test.com");
        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/staff");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await GetResponseAsync<List<StaffSummaryResponse>>(response);
        result.Should().NotBeNull();
        result.Should().BeOfType<List<StaffSummaryResponse>>();
    }

    [Fact]
    public async Task GetProviderStaff_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Owner Salon", "ownerstaff@test.com");

        // Authenticate as different provider
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Salon", "otherstaff@test.com");
        AuthenticateAsProviderOwner(otherProvider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/staff");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetProviderStaff_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Salon", "noauthstaff@test.com");
        ClearAuthenticationHeader();

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/staff");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Add Staff Tests

    [Fact]
    public async Task AddStaff_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Add Staff Salon", "addstaff@test.com");
        AuthenticateAsProviderOwner(provider);

        var request = new AddStaffRequest
        {
            FirstName = "John",
            LastName = "Stylist",
            PhoneNumber = "+1234567890",
            Role = "Stylist",
            Notes = "Senior stylist with 10 years experience"
        };

        // Act
        var response = await PostAsJsonAsync<AddStaffRequest, StaffDetailsResponse>($"/api/v1/providers/{provider.Id.Value}/staff", request);

        // Assert
        response.Error.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().NotBeEmpty();
        response.Data.FirstName.Should().Be("John");
        response.Data.LastName.Should().Be("Stylist");
        response.Data.IsActive.Should().BeTrue();
    }

   

    [Fact]
    public async Task AddStaff_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Owner Salon", "addowner@test.com");

        // Authenticate as different provider
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Salon", "addother@test.com");
        AuthenticateAsProviderOwner(otherProvider);

        var request = new AddStaffRequest
        {
            FirstName = "Hacker",
            LastName = "Staff",
            Email = "hacker@test.com",
            PhoneNumber = "+1234567890",
            Role = "Stylist"
        };

        // Act
        var response = await PostAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/staff", request);

        // Assert
        response.Error.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Update Staff Tests

    [Fact]
    public async Task UpdateStaff_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Update Staff Salon", "updatestaff@test.com");
        AuthenticateAsProviderOwner(provider);

        // Create staff member first
        var createRequest = new AddStaffRequest
        {
            FirstName = "Original",
            LastName = "Name",
            Email = "original@test.com",
            PhoneNumber = "+1234567890",
            Role = "Stylist"
        };

        var createResponse = await PostAsJsonAsync<AddStaffRequest, StaffDetailsResponse>($"/api/v1/providers/{provider.Id.Value}/staff", createRequest);

        // Update staff member
        var updateRequest = new UpdateStaffRequest
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "original@test.com",
            PhoneNumber = "+0987654321",
            Role = "Manager",
            Notes = "Promoted to manager"
        };

        // Act
        var response = await PutAsJsonAsync<UpdateStaffRequest, StaffDetailsResponse>(
            $"/api/v1/providers/{provider.Id.Value}/staff/{createResponse.Data!.Id}",
            updateRequest);

        // Assert
        response.Error.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.FirstName.Should().Be("Updated");
        response.Data.LastName.Should().Be("Name");

        // Phone, role and notes are deliberately NOT echoed, and this test used to assert
        // they were. A membership carries the salon's view of a person — a display name, a
        // per-salon bio and photo, whether they take bookings. It does not carry their phone
        // (that belongs to the Person in UserManagement and the salon may not rewrite it),
        // and roles have their own endpoint (PATCH {id}/roles) because changing them carries
        // the "keep at least one owner" invariant. `notes` is not modelled at all.
        response.Data.PhoneNumber.Should().BeNull(
            "phone is person-level; UpdateMembershipCommand neither accepts nor returns it");
    }

    [Fact]
    public async Task UpdateStaff_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Owner Salon", "updateowner@test.com");
        AuthenticateAsProviderOwner(provider);

        // Create staff member
        var createRequest = new AddStaffRequest
        {
            FirstName = "Test",
            LastName = "Staff",
            Email = "teststaff@test.com",
            PhoneNumber = "+1234567890",
            Role = "Stylist"
        };

        var createResponse = await PostAsJsonAsync<AddStaffRequest, StaffDetailsResponse>($"/api/v1/providers/{provider.Id.Value}/staff", createRequest);

        // Authenticate as different provider
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Salon", "updateother@test.com");
        AuthenticateAsProviderOwner(otherProvider);

        var updateRequest = new UpdateStaffRequest
        {
            FirstName = "Hacked",
            LastName = "Staff",
            Email = "teststaff@test.com",
            PhoneNumber = "+0987654321",
            Role = "Manager"
        };

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/staff/{createResponse.Data!.Id}",
            updateRequest);

        // Assert
        response.Error.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateStaff_WithNonExistentStaffId_ShouldReturn404NotFound()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Salon", "notfoundstaff@test.com");
        AuthenticateAsProviderOwner(provider);

        var nonExistentStaffId = Guid.NewGuid();
        var updateRequest = new UpdateStaffRequest
        {
            FirstName = "Test",
            LastName = "Staff",
            Email = "test@test.com",
            PhoneNumber = "+1234567890",
            Role = "Stylist"
        };

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/staff/{nonExistentStaffId}",
            updateRequest);

        // Assert — 404, as this test's own name and the endpoint's documented responses say.
        // It asserted 400 for as long as the route was a dead 500 (FOLLOW-UPS #16), so the
        // contradiction between the name and the expectation went unnoticed.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

        #region Delete Staff Tests

    [Fact]
    public async Task DeleteStaff_AsOwner_ShouldReturn204NoContent()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Delete Staff Salon", "deletestaff@test.com");
        AuthenticateAsProviderOwner(provider);

        // Create staff member
        var createRequest = new AddStaffRequest
        {
            FirstName = "To",
            LastName = "Delete",
            Email = "todelete@test.com",
            PhoneNumber = "+1234567890",
            Role = "Stylist"
        };

        var createResponse = await PostAsJsonAsync<AddStaffRequest, StaffDetailsResponse>($"/api/v1/providers/{provider.Id.Value}/staff", createRequest);

        // Act
        var response = await DeleteAsync($"/api/v1/providers/{provider.Id.Value}/staff/{createResponse.Data!.Id}");

        // Assert
        response.Error.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify staff is no longer active
        var getResponse = await GetAsync($"/api/v1/providers/{provider.Id.Value}/staff");
        var staffList = await GetResponseAsync<List<StaffSummaryResponse>>(getResponse);
        staffList.Should().NotContain(s => s.Id == createResponse.Data.Id && s.IsActive);
    }

    [Fact]
    public async Task DeleteStaff_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Owner Salon", "deleteowner@test.com");
        AuthenticateAsProviderOwner(provider);

        // Create staff member
        var createRequest = new AddStaffRequest
        {
            FirstName = "Protected",
            LastName = "Staff",
            Email = "protected@test.com",
            PhoneNumber = "+1234567890",
            Role = "Stylist"
        };

        var createResponse = await PostAsJsonAsync<AddStaffRequest, StaffDetailsResponse>($"/api/v1/providers/{provider.Id.Value}/staff", createRequest);

        // Authenticate as different provider
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Salon", "deleteother@test.com");
        AuthenticateAsProviderOwner(otherProvider);

        // Act
        var response = await DeleteAsync($"/api/v1/providers/{provider.Id.Value}/staff/{createResponse.Data!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteStaff_WithNonExistentStaffId_ShouldReturn404NotFound()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Salon", "deletenotfound@test.com");
        AuthenticateAsProviderOwner(provider);

        var nonExistentStaffId = Guid.NewGuid();

        // Act
        var response = await DeleteAsync($"/api/v1/providers/{provider.Id.Value}/staff/{nonExistentStaffId}");

        // Assert — 404, which is what this test's own name says and what the endpoint
        // documents. It asserted 400 for as long as the route was a dead 500, so nobody
        // noticed the expectation contradicted the name.
        response.Error.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Complete Staff Workflow Test

    [Fact]
    public async Task StaffCompleteWorkflow_CreateUpdateDelete_ShouldWorkEndToEnd()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Workflow Staff Salon", "workflowstaff@test.com");
        AuthenticateAsProviderOwner(provider);

        // Step 1: Create staff member
        var createRequest = new AddStaffRequest
        {
            FirstName = "Workflow",
            LastName = "Staff",
            Email = "workflow@test.com",
            PhoneNumber = "+1234567890",
            Role = "Stylist",
            Notes = "Initial notes"
        };

        var createResponse = await PostAsJsonAsync<AddStaffRequest, StaffDetailsResponse>($"/api/v1/providers/{provider.Id.Value}/staff", createRequest);
        createResponse.Error.Should().BeNull();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Data.Should().NotBeNull();

        // Step 2: Update staff member
        var updateRequest = new UpdateStaffRequest
        {
            FirstName = "Updated Workflow",
            LastName = "Staff Updated",
            Email = "workflow@test.com",
            PhoneNumber = "+0987654321",
            Role = "Manager",
            Notes = "Updated to manager"
        };

        var updateResponse = await PutAsJsonAsync<UpdateStaffRequest, StaffDetailsResponse>(
            $"/api/v1/providers/{provider.Id.Value}/staff/{createResponse.Data!.Id}",
            updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // A member without an app account has ONE display name at this salon. The endpoint
        // joins the request's first/last into it and splits it back on the FIRST space, so a
        // two-word "first name" does not survive the round trip: "Updated Workflow" + "Staff
        // Updated" stores "Updated Workflow Staff Updated" and returns First="Updated".
        // That is the real contract of a single display name, not a bug to paper over — the
        // legacy first/last shape is what does not fit. Role is not updatable here at all
        // (PATCH {id}/roles owns it, because of the keep-one-owner invariant).
        updateResponse.Data!.FirstName.Should().Be("Updated");
        updateResponse.Data.LastName.Should().Be("Workflow Staff Updated");

        // Step 3: Get all staff and verify our staff is in the list
        var getResponse = await GetAsync($"/api/v1/providers/{provider.Id.Value}/staff");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staffList = await GetResponseAsync<List<StaffSummaryResponse>>(getResponse);
        staffList.Should().Contain(s => s.Id == createResponse.Data.Id);

        // Step 4: Delete staff member
        var deleteResponse = await DeleteAsync($"/api/v1/providers/{provider.Id.Value}/staff/{createResponse.Data.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Step 5: Verify staff is no longer active
        var finalGetResponse = await GetAsync($"/api/v1/providers/{provider.Id.Value}/staff");
        var finalStaffList = await GetResponseAsync<List<StaffSummaryResponse>>(finalGetResponse);
        finalStaffList.Should().NotContain(s => s.Id == createResponse.Data.Id && s.IsActive);
    }

    #endregion
}
