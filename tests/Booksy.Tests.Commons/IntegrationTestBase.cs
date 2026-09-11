using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.Tests.Commons;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace Booksy.Tests.Common.Infrastructure;

/// <summary>
/// Base class for all integration tests that need full API and database access.
///
/// <c>TFactory</c> is constrained to <see cref="TestWebApplicationFactory{TStartup,TDbContext}"/>,
/// not any <c>WebApplicationFactory</c>, so this class can call <c>Factory.ResetStateAsync()</c>.
/// The composition tests (<c>HostCompositionFactory</c>) do not derive from this base and are
/// unaffected by the tighter constraint.
///
/// <para>Deliberately does NOT declare <c>IClassFixture&lt;TFactory&gt;</c> — that would give every
/// concrete test class its own <c>TFactory</c> instance regardless of which xUnit collection it
/// joins, which is exactly the per-class host boot docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 2
/// removes for the ServiceCatalog suite (one <c>[CollectionDefinition]</c> +
/// <c>ICollectionFixture&lt;TFactory&gt;</c> instead; see <c>ServiceCatalogTestCollection</c>). A
/// suite that has not moved to a collection yet (UserManagement, until slice 3) must declare
/// <c>IClassFixture&lt;TFactory&gt;</c> itself on its own non-generic base — xUnit resolves fixture
/// interfaces from the full inheritance chain, so declaring it one level down works identically to
/// declaring it here.</para>
/// </summary>
public abstract class IntegrationTestBase<TFactory, TDbContext, TStartup>
    : IAsyncLifetime
    where TFactory : TestWebApplicationFactory<TStartup, TDbContext>
    where TDbContext : DbContext
    where TStartup : class
{
    public readonly TFactory Factory;
    public readonly HttpClient Client;
    public IServiceScope Scope;
    public TDbContext DbContext;
    public TestUserContext _userContext;


    public IntegrationTestBase(TFactory factory)
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

        // Start every test unauthenticated.
        //
        // TestUserContext is registered as a SINGLETON on the factory, so it is shared by every test in
        // the class. Without this reset, whoever the previous test signed in as is still signed in, and a
        // test that never calls an Authenticate* helper silently inherits that identity. That is exactly
        // why UploadProfileImage_WithoutAuthentication and UpdateProfile_WithoutAuthentication returned
        // 200/204 instead of 401: they correctly send no credentials, but the shared context still held a
        // user from an earlier test. Tests that need an identity establish it explicitly, so clearing here
        // is safe and makes the "without authentication" cases independent of execution order.
        _userContext.ClearUser();
        Client.DefaultRequestHeaders.Authorization = null;

        // Reset the database, the caches and every capturing fake before the test body runs. This
        // used to be CleanDatabaseAsync(), a hook every derived base declared and neither ever
        // implemented (docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 1) — the database accumulated
        // rows across a whole class's tests, which is exactly what made a shared host unsafe.
        await Factory.ResetStateAsync();
    }

    /// <summary>
    /// xUnit lifecycle - Cleanup after each test
    /// </summary>
    public virtual Task DisposeAsync()
    {
        // Nothing to do here: the NEXT test's InitializeAsync resets state before it runs, and if
        // this was the class's last test there is no next test to protect from stale rows.
        Scope?.Dispose();
        return Task.CompletedTask;
    }

    // ================================================
    // DATABASE HELPERS
    // ================================================

    public async Task<T?> FindEntityAsync<T>(Guid id) where T : class
    {
        return await DbContext.Set<T>().FindAsync(id);
    }

    public async Task<T?> FindEntityAsync<T>(Func<T, bool> predicate) where T : class
    {
        return DbContext.Set<T>().FirstOrDefault(predicate);
    }

    public async Task CreateEntityAsync<T>(T entity) where T : class
    {
        await DbContext.Set<T>().AddAsync(entity);
        await DbContext.SaveChangesAsync();
    }

    public async Task CreateEntitiesAsync<T>(params T[] entities) where T : class
    {
        await DbContext.Set<T>().AddRangeAsync(entities);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateEntityAsync<T>(T entity) where T : class
    {
        // Check if entity is already being tracked
        var entry = DbContext.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            DbContext.Set<T>().Update(entity);
        }

        await DbContext.SaveChangesAsync();
    }

    public void SetPrivateProperty<T>(T entity, string propertyName, object value)
    {
        var property = typeof(T).GetProperty(propertyName,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        property?.SetValue(entity, value);
    }

    public async Task<ApiResponse> PostAsync(string url)
    {

        var result = await Client.PostAsync(url, null);
        var content = await result.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(content))
        {
            return new Core.Domain.Infrastructure.Middleware.ApiResponse
            {
                StatusCode = result.StatusCode,
            };
        }
        var response = JsonConvert.DeserializeObject<ApiResponse>(content);
        if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            throw new Exception($"Error Message: {response.Message} \n Erros: {response.Errors}");

        return response;


    }
    public async Task<ApiResponse> PostAsJsonAsync<T>(string url, T data)
    {

          var result = await Client.PostAsJsonAsync(url, data);
            var content = await result.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                return new Core.Domain.Infrastructure.Middleware.ApiResponse
                {
                    StatusCode = result.StatusCode,
                };
            }
            var response = JsonConvert.DeserializeObject<ApiResponse>(content);
            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                throw new Exception($"Error Message: {response.Message} \n Erros: {response.Errors}");

            return response;
        

    }

    public async Task<ApiResponse<TResponse>> PostAsJsonAsync<T, TResponse>(string url, T data)
    {

        var result = await Client.PostAsJsonAsync(url, data);
            var content = await result.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                // Was `return new ApiResponse<TResponse> { }` — an empty response body silently
                // discarded the REAL HTTP status code, leaving every field (including StatusCode)
                // at its default: 0. Callers asserting on StatusCode saw "0" regardless of what the
                // server actually returned (200, 400, 404, ...), which is indistinguishable from a
                // request that never completed. Preserving result.StatusCode here does not change
                // any currently-passing assertion — it only replaces a meaningless placeholder with
                // the value the response line above already has in hand.
                return new ApiResponse<TResponse>
                {
                    StatusCode = result.StatusCode
                };
            }
            var response = JsonConvert.DeserializeObject<ApiResponse<TResponse>>(content);
            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                throw new Exception($"Error Message: {response.Message} \n Erros: {response.Errors}");

            return response;
       

    }

    public async Task<ApiResponse<TResponse>> PutAsJsonAsync<T, TResponse>(string url, T data)
    {
       
            var result = await Client.PutAsJsonAsync(url, data);
            var content = await result.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                // Was `return new ApiResponse<TResponse> { }` — an empty response body silently
                // discarded the REAL HTTP status code, leaving every field (including StatusCode)
                // at its default: 0. Callers asserting on StatusCode saw "0" regardless of what the
                // server actually returned (200, 400, 404, ...), which is indistinguishable from a
                // request that never completed. Preserving result.StatusCode here does not change
                // any currently-passing assertion — it only replaces a meaningless placeholder with
                // the value the response line above already has in hand.
                return new ApiResponse<TResponse>
                {
                    StatusCode = result.StatusCode
                };
            }
            var response = JsonConvert.DeserializeObject<ApiResponse<TResponse>>(content);
            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                throw new Exception($"Error Message: {response.Message} \n Erros: {response.Errors}");

            return response;
       
    }


    public async Task<ApiResponse> PutAsJsonAsync<T>(string url, T data)
    {
       
            var result = await Client.PutAsJsonAsync(url, data);
            var content = await result.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                return new Core.Domain.Infrastructure.Middleware.ApiResponse
                {
                    StatusCode = result.StatusCode,
                };
            }
            var response = JsonConvert.DeserializeObject<Core.Domain.Infrastructure.Middleware.ApiResponse>(content);
            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                throw new Exception($"Error Message: {response.Message} \n Erros: {response.Errors}");

            return response;
       
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        return await Client.GetAsync(url);

    }
    public async Task<ApiResponse<TResponse>> GetAsync<TResponse>(string url)
    {

        var result = await Client.GetAsync(url);
        var content = await result.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(content))
        {
            return new ApiResponse<TResponse>
            {
                StatusCode = result.StatusCode,
            };
        }
        var response = JsonConvert.DeserializeObject<ApiResponse<TResponse>>(content);
        if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            throw new Exception($"Error Message: {response.Message} \n Erros: {response.Errors}");
        return response;
    }

    public async Task<ApiResponse> DeleteAsync(string url)
    {

      
            var result = await Client.DeleteAsync(url); ;
            var content = await result.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
            return new Core.Domain.Infrastructure.Middleware.ApiResponse
                {
                    StatusCode = result.StatusCode,
                };
            }

            
            var response = JsonConvert.DeserializeObject<ApiResponse>(content);
            if(response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                throw new Exception($"Error Message: {response.Message} \n Erros: {response.Errors}");
            return response;
      

    }

    public async Task<TResponse?> GetResponseAsync<TResponse>(HttpResponseMessage response)
    {

        var reson = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ApiResponse<TResponse>>(reson);

        return result.Data;
    }



    /// <summary>
    /// Authenticate as a customer
    /// </summary>
    public TestUser AuthenticateAsCustomer(string email)
    {
        var user = TestUser.Customer(email);

        _userContext.SetUser(user);

        return user;
    }

    /// <summary>
    /// Authenticate as a customer
    /// </summary>
    public TestUser AuthenticateAsCustomer(Guid userId, string email = "customer@test.com")
    {
        var user = TestUser.Customer(email, userId);

        _userContext.SetUser(user);

        return user;
    }
    /// <summary>
    /// Authenticate as a provider
    /// </summary>
    public TestUser AuthenticateAsProvider(string email = "provider@test.com", string providerId = null)
    {
        var user = TestUser.Provider(email, providerId);
        _userContext.SetUser(user);

        return user;
    }

    /// <summary>
    /// Authenticate as an admin
    /// </summary>
    public TestUser AuthenticateAsAdmin(string email = "admin@test.com")
    {
        var user = TestUser.Admin(email);
        _userContext.SetUser(user);

        return user;
    }

    /// <summary>
    /// Authenticate with custom test user
    /// </summary>
    public void AuthenticateAs(TestUser user)
    {
        _userContext.SetUser(user);
    }


    /// <summary>
    /// Authenticate with custom test user
    /// </summary>
    public void LogOut()
    {
        _userContext.SetUser(null);
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


    public static void AssertSuccessStatusCode(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
    }

    public static void AssertStatusCode(HttpResponseMessage response, System.Net.HttpStatusCode expectedStatusCode)
    {
        //FluentAssertions.AssertionExtensions.Should((int)response.StatusCode)
        //    .Be((int)expectedStatusCode);
    }
}


// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);


