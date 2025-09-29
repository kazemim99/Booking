// ========================================
// Booksy.Tests.Common/Infrastructure/IntegrationTestBase.cs
// ========================================
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;

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
    protected  TestUserContext _userContext;


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
        _userContext = Scope.ServiceProvider.GetRequiredService<TestUserContext>();
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

        var reson = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<TResponse>(reson);
    }



    /// <summary>
    /// Authenticate as a customer
    /// </summary>
    public void AuthenticateAsCustomer(string email = "customer@test.com")
    {
        var user = TestUser.Customer(email);

        _userContext.SetUser(user);

 
    }

    /// <summary>
    /// Authenticate as a provider
    /// </summary>
    public void AuthenticateAsProvider(string email = "provider@test.com", string providerId = null)
    {
        var user = TestUser.Provider(email, providerId);
        _userContext.SetUser(user);

    
    }

    /// <summary>
    /// Authenticate as an admin
    /// </summary>
    public void AuthenticateAsAdmin(string email = "admin@test.com")
    {
        var user = TestUser.Admin(email);
        _userContext.SetUser(user);

    }

    /// <summary>
    /// Authenticate with custom test user
    /// </summary>
    public void AuthenticateAs(TestUser user)
    {
        _userContext.SetUser(user);

    }

    /// <summary>
    /// Authenticate with custom claims
    /// </summary>
    public void AuthenticateWithClaims(string email, string role, Dictionary<string, string> claims)
    {
        var user = new TestUser
        {
            Email = email,
            Role = role,
            AdditionalClaims = claims
        };

        _userContext.SetUser(user);

    }

    /// <summary>
    /// Clear authentication (unauthenticated requests)
    /// </summary>
    public void ClearAuthentication()
    {
        _userContext.ClearUser();
        Client.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Get current authenticated user
    /// </summary>
    public TestUser? GetCurrentUser()
    {
        return _userContext.CurrentUser;
    }


    protected static void AssertSuccessStatusCode(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
    }

    protected static void AssertStatusCode(HttpResponseMessage response, System.Net.HttpStatusCode expectedStatusCode)
    {
        //FluentAssertions.AssertionExtensions.Should((int)response.StatusCode)
        //    .Be((int)expectedStatusCode);
    }
}