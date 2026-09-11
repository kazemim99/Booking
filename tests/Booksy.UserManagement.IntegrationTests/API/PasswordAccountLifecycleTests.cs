using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Booksy.Core.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Booksy.UserManagement.IntegrationTests.API;

/// <summary>
/// Regression guard for the lost-write class of bug in UserManagement: handlers that wrote through
/// <c>IUserRepository.UpdateAsync/SaveAsync</c> (which only track) and relied on the pipeline's
/// TransactionBehavior — which commits the ServiceCatalog unit of work, registered last, not this
/// context's. Registration, password login, refresh-token rotation, password change and password
/// reset all silently discarded their writes. Every step here proves persistence through the real
/// HTTP pipeline by reading back from the database or by making the next request depend on it.
/// </summary>
[Collection(UserManagementTestCollection.Name)]
public class PasswordAccountLifecycleTests : UserManagementIntegrationTestBase
{
    private const string Password = "Str0ng!Pass";
    private const string NewPassword = "Even-Str0nger!";

    public PasswordAccountLifecycleTests(UserManagementTestWebApplicationFactory<Startup> factory)
        : base(factory) { }

    private static string Field(JsonElement root, string name)
    {
        // The API envelope puts payloads under "data"; tolerate both shapes.
        var payload = root.TryGetProperty("data", out var data) ? data : root;
        return payload.GetProperty(name).GetString()!;
    }

    private async Task<(Guid UserId, string Email)> RegisterAsync()
    {
        var email = $"lifecycle-{Guid.NewGuid():N}@test.com";
        ClearAuthenticationHeader();
        var response = await Client.PostAsJsonAsync("/api/v1/users", new
        {
            email,
            password = Password,
            firstName = "Life",
            lastName = "Cycle",
            phoneNumber = $"+9891{Random.Shared.Next(10000000, 99999999)}",
            userType = "Customer",
            acceptTerms = true,
            marketingConsent = false,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var payload = body.TryGetProperty("data", out var d) ? d : body;
        var userId = payload.TryGetProperty("userId", out var uid)
            ? uid.GetGuid()
            : payload.GetProperty("id").GetGuid();

        // Registration leaves the account Pending until the e-mail activation token is used;
        // that flow is not what these tests are about, so activate directly in the database.
        DbContext.ChangeTracker.Clear();
        var user = await DbContext.Users.FirstAsync(u => u.Id == UserId.From(userId));
        user.SetStatus(Booksy.UserManagement.Domain.Enums.UserStatus.Active);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        return (userId, email);
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password, rememberMe = false });

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        Client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });

    [Fact]
    public async Task Registration_Is_Persisted()
    {
        var (userId, email) = await RegisterAsync();

        DbContext.ChangeTracker.Clear();
        var persisted = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == UserId.From(userId));
        persisted.Should().NotBeNull("the registration handler must commit its own unit of work");
        persisted!.Email.Value.Should().Be(email);
    }

    [Fact]
    public async Task Login_Then_Refresh_Rotates_And_Persists_Refresh_Tokens()
    {
        var (userId, email) = await RegisterAsync();

        var login = await LoginAsync(email, Password);
        login.StatusCode.Should().Be(HttpStatusCode.OK, await login.Content.ReadAsStringAsync());
        var loginBody = JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement;
        var firstRefresh = Field(loginBody, "refreshToken");

        firstRefresh.Should().NotBeNullOrWhiteSpace("login must hand out a refresh token");

        DbContext.ChangeTracker.Clear();
        var afterLogin = await DbContext.Users.Include(u => u.RefreshTokens)
            .FirstAsync(u => u.Id == UserId.From(userId));
        afterLogin.RefreshTokens.Should().Contain(t => t.Token == firstRefresh,
            "the login handler must have committed the token it handed out — a failure of the " +
            "best-effort membership lookup used to abort the request's transaction and roll this back");

        // The login handler stores the refresh token it hands out; refresh must find it.
        var refresh = await RefreshAsync(firstRefresh);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK, await refresh.Content.ReadAsStringAsync());
        var secondRefresh = Field(JsonDocument.Parse(await refresh.Content.ReadAsStringAsync()).RootElement, "refreshToken");
        secondRefresh.Should().NotBe(firstRefresh, "refresh rotates the token");

        // Rotation must be durable: the old token is revoked, the new one is usable.
        (await RefreshAsync(firstRefresh)).StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "a rotated-away refresh token must be revoked in the database, not only in memory");
        (await RefreshAsync(secondRefresh)).StatusCode.Should().Be(HttpStatusCode.OK,
            "the token issued by the previous refresh must have been stored");

        DbContext.ChangeTracker.Clear();
        var user = await DbContext.Users.Include(u => u.RefreshTokens)
            .FirstAsync(u => u.Id == UserId.From(userId));
        user.RefreshTokens.Should().HaveCountGreaterThanOrEqualTo(3, "login + two refreshes each persisted a token");
        user.RefreshTokens.Count(t => t.RevokedAt != null).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Change_Password_Is_Persisted_And_Old_Password_Stops_Working()
    {
        var (userId, email) = await RegisterAsync();

        AuthenticateAsCustomerWithId(userId, email);
        var change = await Client.PostAsJsonAsync($"/api/v1/users/{userId}/change-password", new
        {
            currentPassword = Password,
            newPassword = NewPassword,
            confirmNewPassword = NewPassword,
        });
        change.StatusCode.Should().Be(HttpStatusCode.OK, await change.Content.ReadAsStringAsync());

        ClearAuthenticationHeader();
        (await LoginAsync(email, NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK,
            "the new password hash must be committed");
        (await LoginAsync(email, Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "the old password must no longer authenticate");
    }

    [Fact]
    public async Task Forgot_Password_For_An_Unknown_Email_Does_Not_Reveal_It()
    {
        ClearAuthenticationHeader();
        var response = await Client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new { email = $"nobody-{Guid.NewGuid():N}@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "an unknown address must get the same answer as a known one (it used to dereference null and return 500)");
    }
}
