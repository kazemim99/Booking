// ========================================
// Booksy.Tests.Common/Infrastructure/IntegrationTestBase.cs
// ========================================
using Booksy.Tests.Common.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace Booksy.Tests.Common.Infrastructure;

/// <summary>
/// Base class for all integration tests that need full API and database access
/// </summary>
public abstract class IntegrationTestBase<TFactory, TDbContext, TStartup>
    : IClassFixture<TFactory>,
      IAsyncLifetime
    where TFactory : WebApplicationFactory<TStartup>
    where TDbContext : DbContext
    where TStartup : class
{
    protected readonly TFactory Factory;
    protected readonly HttpClient Client;
    protected IServiceScope Scope;
    protected TDbContext DbContext;

    protected IntegrationTestBase(TFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    /// <summary>
    /// xUnit lifecycle - Initialize before each test
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<TDbContext>();

        // Clean database before each test
        await CleanDatabaseAsync();
    }

    /// <summary>
    /// xUnit lifecycle - Cleanup after each test
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        await CleanDatabaseAsync();
        Scope?.Dispose();
    }

    // ================================================
    // DATABASE HELPERS
    // ================================================

    protected virtual async Task CleanDatabaseAsync()
    {
        // Override in derived classes for specific cleanup logic
        // Example: await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM Services");
        await Task.CompletedTask;
    }

    protected async Task<T?> FindEntityAsync<T>(Guid id) where T : class
    {
        return await DbContext.Set<T>().FindAsync(id);
    }

    protected async Task<T?> FindEntityAsync<T>(Func<T, bool> predicate) where T : class
    {
        return DbContext.Set<T>().FirstOrDefault(predicate);
    }

    protected async Task CreateEntityAsync<T>(T entity) where T : class
    {
        await DbContext.Set<T>().AddAsync(entity);
        await DbContext.SaveChangesAsync();
    }

    protected async Task CreateEntitiesAsync<T>(params T[] entities) where T : class
    {
        await DbContext.Set<T>().AddRangeAsync(entities);
        await DbContext.SaveChangesAsync();
    }

    protected void SetPrivateProperty<T>(T entity, string propertyName, object value)
    {
        var property = typeof(T).GetProperty(propertyName,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        property?.SetValue(entity, value);
    }

    // ================================================
    // HTTP CLIENT HELPERS
    // ================================================

    protected void SetAuthenticationHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    protected void ClearAuthenticationHeader()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    protected async Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T data)
    {
        return await Client.PostAsJsonAsync(url, data);
    }

    protected async Task<HttpResponseMessage> PutAsJsonAsync<T>(string url, T data)
    {
        return await Client.PutAsJsonAsync(url, data);
    }

    protected async Task<HttpResponseMessage> GetAsync(string url)
    {
        return await Client.GetAsync(url);
    }

    protected async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        return await Client.DeleteAsync(url);
    }

    protected async Task<TResponse?> GetResponseAsync<TResponse>(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    // ================================================
    // AUTHENTICATION HELPERS
    // ================================================

    protected void AuthenticateAsUser(string userId, string email)
    {
        var context = TestAuthenticationContext.CreateUser(userId, email);
        SetAuthenticationHeaders(context);
    }

    protected void AuthenticateAsProvider(string providerId, string email)
    {
        var context = TestAuthenticationContext.CreateProvider(providerId, email);
        SetAuthenticationHeaders(context);
    }

    protected void AuthenticateAsAdmin(string userId, string email)
    {
        var context = TestAuthenticationContext.CreateAdmin(userId, email);
        SetAuthenticationHeaders(context);
    }

    protected void AuthenticateAs(TestAuthenticationContext context)
    {
        SetAuthenticationHeaders(context);
    }

    protected void AuthenticateWithRoles(string userId, string email, params string[] roles)
    {
        var context = TestAuthenticationContext.CreateUser(userId, email);
        context.WithRoles(roles);
        SetAuthenticationHeaders(context);
    }

    private void SetAuthenticationHeaders(TestAuthenticationContext context)
    {
        ClearAuthenticationHeader(); // Clear any existing auth

        foreach (var header in context.ToHeaders())
        {
            Client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    // ================================================
    // ASSERTION HELPERS
    // ================================================

    protected static void AssertSuccessStatusCode(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
    }

    protected static void AssertStatusCode(HttpResponseMessage response, System.Net.HttpStatusCode expectedStatusCode)
    {
        FluentAssertions.AssertionExtensions.Should((int)response.StatusCode)
            .Be((int)expectedStatusCode);
    }
}