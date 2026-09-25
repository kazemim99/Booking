using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.DTOs;
using AsanRezerve.Infrastructure.Core.Caching;
using AsanRezerve.ServiceCatalog.Application.Caching;
using AsanRezerve.ServiceCatalog.Application.DTOs.Provider;
using AsanRezerve.ServiceCatalog.Application.Queries.Category.GetCategoriesWithCounts;
using AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetProviderById;
using AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetRegistrationProgress;
using AsanRezerve.ServiceCatalog.Application.Queries.Provider.SearchProviders;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AsanRezerve.ServiceCatalog.Application.UnitTests.Caching;

/// <summary>
/// The reads the customer apps make most — the salon page, salon lists, categories — are served from the query
/// cache, each tagged with what evicts it.
/// <para>And what the cache stores must come back whole. The provider aggregate cache this replaced could not be
/// read back at all (private setters; FOLLOW-UPS #70); a read model with an <c>internal set</c> would lose that
/// member just as silently on every cache hit.</para>
/// </summary>
public sealed class ReadModelCachingTests : IAsyncDisposable
{
    private readonly ServiceProvider _services = new ServiceCollection()
        .AddLogging()
        .AddAsanRezerveCaching(new ConfigurationBuilder()
            .AddInMemoryCollection([new("Cache:Provider", "InMemory")])
            .Build())
        .BuildServiceProvider();

    public async ValueTask DisposeAsync() => await _services.DisposeAsync();

    private static IQuery<T> AsQuery<T>(IQuery<T> query) => query;

    [Fact]
    public void TheSalonPage_IsCachedForFiveMinutes_UnderItsProvidersTag()
    {
        var id = Guid.NewGuid();
        var query = AsQuery(new GetProviderByIdQuery(id, IncludeServices: true, IncludeStaff: true));

        query.IsCacheable.Should().BeTrue();
        query.CacheExpirationSeconds.Should().Be(300);
        query.CacheTags.Should().BeEquivalentTo([ReadModelCacheTags.Provider(id)]);
        query.CacheKey.Should().BeNull("the include flags change the result, so the whole query is the key");
    }

    [Fact]
    public void ASalonList_IsCachedForAMinute_UnderTheDirectoryTag()
    {
        var query = AsQuery(new SearchProvidersQuery(City: "تهران", SortBy: "rating"));

        query.IsCacheable.Should().BeTrue();
        query.CacheExpirationSeconds.Should().Be(60);
        query.CacheTags.Should().BeEquivalentTo([ReadModelCacheTags.ProviderDirectory]);
        query.CacheKey.Should().BeNull();
    }

    [Fact]
    public void ASalonListAroundTheUsersPosition_IsNotCached()
    {
        // Every user stands somewhere else: entries keyed on coordinates would almost never be hit again and would
        // push the useful ones out of memory.
        var query = AsQuery(new SearchProvidersQuery(SortBy: "distance", UserLatitude: 35.7, UserLongitude: 51.4));

        query.IsCacheable.Should().BeFalse();
    }

    [Fact]
    public void Categories_AreCachedForTenMinutes_UnderTheCategoriesTag()
    {
        var query = AsQuery(new GetCategoriesWithCountsQuery(Limit: 10, OnlyPopular: true));

        query.IsCacheable.Should().BeTrue();
        query.CacheExpirationSeconds.Should().Be(600);
        query.CacheTags.Should().BeEquivalentTo([ReadModelCacheTags.Categories]);
    }

    [Fact]
    public void AProviderChange_EvictsItsPage_TheLists_AndTheCategoryCounts()
    {
        var id = Guid.NewGuid();

        ReadModelCacheTags.ForProviderChange(id).Should().BeEquivalentTo(
            [$"provider:{id}", "provider-directory", "categories"]);
    }

    private async Task<T> RoundTrip<T>(T value)
    {
        var cache = _services.GetRequiredService<HybridCache>();
        var key = Guid.NewGuid().ToString();

        await cache.GetOrCreateAsync(key, _ => ValueTask.FromResult(value));

        // The second read is served from the cache: L1 keeps the serialised bytes and deserialises a fresh copy.
        return await cache.GetOrCreateAsync<T>(key, _ => throw new InvalidOperationException("expected a cache hit"));
    }

    [Fact]
    public async Task TheSalonPage_ComesBackWhole()
    {
        var page = new ProviderDetailsResult
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            BusinessName = "سالن نهال",
            Description = "آرایشگاه زنانه",
            PrimaryCategory = ServiceCategory.HairSalon,
            Status = ProviderStatus.Active,
            ContactInfo = new ContactInfo("a@b.ir", "09121234567", null, "https://nahal.ir"),
            Address = new AddressInfo("ولیعصر", "تهران", "تهران", 1, 8, "12345", "IR", 35.7, 51.4) { FormattedAddress = "تهران، ولیعصر" },
            BusinessHours =
            [
                new BusinessHoursData(6, true, 9, 0, 18, 30, [new BreakPeriodData(13, 0, 14, 0, "ناهار")]),
            ],
            Images = [new ProviderImageItem(Guid.NewGuid(), "t.jpg", "m.jpg", "o.jpg", true, 0)],
            LogoUrl = "https://x/logo.jpg",
            MaxAdvanceBookingDays = 30,
            AverageRating = 4.5m,
            TotalReviews = 12,
            ServiceCount = 3,
            Tags = ["رنگ مو", "کوتاهی"],
            RegisteredAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Services =
            [
                new ProviderServiceItem { Id = Guid.NewGuid(), Name = "کوتاهی", BasePrice = 500_000, Currency = "IRR", Duration = 45, Status = ServiceStatus.Active, Type = "Standard", ImageUrl = "s.jpg" },
            ],
        };
        page.Staff.Add(new ProviderStaffItem { Id = Guid.NewGuid(), FirstName = "مریم", LastName = "احمدی", FullName = "مریم احمدی", Role = StaffRole.ServiceProvider, IsActive = true });
        // internal set: the handler assigns it after construction, as the test does here through reflection.
        typeof(ProviderDetailsResult).GetProperty(nameof(ProviderDetailsResult.ActiveServicesCount))!.SetValue(page, 1);

        var copy = await RoundTrip(page);

        copy.Should().NotBeSameAs(page);
        copy.Should().BeEquivalentTo(page);
    }

    [Fact]
    public async Task ASalonListPage_ComesBackWhole()
    {
        var list = new PagedResult<ProviderSearchItem>(
            [
                new ProviderSearchItem(Guid.NewGuid(), "سالن نهال", "توضیح", "p.jpg", ServiceCategory.HairSalon, ProviderStatus.Active,
                    "تهران", "تهران", "IR", "l.jpg", true, false, 4.5m, 3, 2, true,
                    new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, staffMemberCount: 4, totalReviews: 12),
            ],
            totalCount: 41, pageNumber: 2, pageSize: 20);

        var copy = await RoundTrip(list);

        copy.Should().BeEquivalentTo(list);
    }

    [Fact]
    public async Task Categories_ComeBackWhole()
    {
        var categories = new List<CategoryWithCountViewModel>
        {
            new() { Id = 1, Key = "hair_salon", Name = "آرایشگاه", EnglishName = "Hair salon", Slug = "hair-salon", Icon = "✂", ProviderCount = 7, DisplayOrder = 1 },
        };

        var copy = await RoundTrip(categories);

        copy.Should().BeEquivalentTo(categories);
    }
}
