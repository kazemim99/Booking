using System.Reflection;
using System.Runtime.CompilerServices;
using AsanRezerve.Core.Application.Abstractions.CQRS;
using FluentAssertions;

namespace AsanRezerve.ArchitectureTests;

/// <summary>
/// Which queries may be served from the query cache.
/// <para>A cached query is served stale for its whole lifetime unless something evicts it, so caching is a
/// decision per query: the result must be the same for every caller allowed to run it, and every change to it
/// must evict its tags. The list below is that decision, in one place. Adding a query to it means naming what
/// invalidates it.</para>
/// <para>Five queries used to cache with sliding expiration and no invalidation at all — so the more often a
/// screen was opened, the longer it stayed wrong: the availability calendar kept offering booked slots, and a
/// customer's profile edits and favourites did not show. They are listed as never-cacheable.</para>
/// </summary>
public class QueryCachePolicyTests
{
    private static readonly Assembly[] ApplicationAssemblies =
    [
        typeof(AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetProviderById.GetProviderByIdQuery).Assembly,
        typeof(AsanRezerve.UserManagement.Application.CQRS.Queries.GetUserById.GetUserByIdQuery).Assembly,
    ];

    /// <summary>Cacheable queries, each with what evicts it (design D8 of add-observability-and-caching).</summary>
    private static readonly string[] Allowed =
    [
        "GetProviderByIdQuery",          // provider:{id} — every save of the salon, its services or its staff
        "SearchProvidersQuery",          // provider-directory — any provider change; 60 s; never around a position
        "GetCategoriesWithCountsQuery",  // categories — every provider change
    ];

    private static readonly string[] NeverCacheable =
    [
        "GetProviderAvailabilityCalendarQuery", // bookings change it every minute; a stale slot is a failed booking
        "GetCustomerByIdQuery",                 // personal, edited by its owner, nothing evicts it
        "GetCustomerFavoriteProvidersQuery",    // personal, edited by its owner, nothing evicts it
        "GetUserByIdQuery",                     // personal, edited by its owner, nothing evicts it
        "SearchUsersQuery",                     // admin search; its hand-built key dropped five filters
    ];

    private static IEnumerable<Type> QueryTypes() =>
        ApplicationAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false })
            .Where(t => QueryInterface(t) is not null);

    private static Type? QueryInterface(Type t) =>
        t.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));

    /// <summary>
    /// Evaluates <c>IsCacheable</c> the way the pipeline does, through the interface. Queries are records of
    /// plain values; an uninitialised instance answers for the default (no-coordinates, no-filter) case.
    /// </summary>
    private static bool IsCacheable(Type queryType)
    {
        var instance = RuntimeHelpers.GetUninitializedObject(queryType);
        var property = QueryInterface(queryType)!.GetProperty(nameof(IQuery<object>.IsCacheable))!;
        return (bool)property.GetValue(instance)!;
    }

    [Fact]
    public void Only_listed_queries_are_cacheable()
    {
        var cacheable = QueryTypes().Where(IsCacheable).Select(t => t.Name).ToList();

        cacheable.Should().BeSubsetOf(Allowed,
            "a cacheable query must be the same for every caller and evicted by tag on every change; " +
            "add it to this list together with what invalidates it");
    }

    [Fact]
    public void Queries_whose_data_changes_without_eviction_are_never_cached()
    {
        var types = QueryTypes().Where(t => NeverCacheable.Contains(t.Name)).ToList();

        types.Select(t => t.Name).Should().BeEquivalentTo(NeverCacheable, "each of them must still exist to be checked");
        types.Where(IsCacheable).Select(t => t.Name).Should().BeEmpty();
    }

    [Fact]
    public void Cache_members_have_the_interface_types_so_they_are_not_silently_ignored()
    {
        // `public int CacheExpirationSeconds => 300` does NOT implement `int? CacheExpirationSeconds`: the default
        // interface member (null) wins and the declared value is dead. Three queries shipped exactly that.
        var expected = new Dictionary<string, Type>
        {
            ["IsCacheable"] = typeof(bool),
            ["CacheKey"] = typeof(string),
            ["CacheExpirationSeconds"] = typeof(int?),
            ["CacheTags"] = typeof(IReadOnlyCollection<string>),
        };

        var mismatches =
            from type in QueryTypes()
            from property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            where expected.TryGetValue(property.Name, out var want) && property.PropertyType != want
            select $"{type.Name}.{property.Name} is {property.PropertyType.Name}, expected {expected[property.Name].Name}";

        mismatches.Should().BeEmpty();
    }
}
