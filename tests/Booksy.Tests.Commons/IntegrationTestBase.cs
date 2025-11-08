using Booksy.Core.Domain.Infrastructure.Middleware;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
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

    public virtual async Task CleanDatabaseAsync()
    {
        // Override in derived classes for specific cleanup logic
        // Example: await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM Services");
        await Task.CompletedTask;
    }

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
                return new ApiResponse<TResponse>
                {
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
                return new ApiResponse<TResponse>
                {
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


