using System.Net;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.Tests.Common;
using FluentAssertions;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Reviews;

/// <summary>
/// The one-off recompute run after the moderation migration, so every provider's rating becomes true for the
/// first time (task 4.7, design Migration Plan step 3).
/// </summary>
/// <remarks>
/// <para>Before this change nothing wrote <c>Provider.AverageRating</c>, so every provider in production reads 0
/// with a count of 0, however many reviews it has. The migration publishes those reviews but does not — and
/// cannot, without restating the rating rule in SQL — compute the aggregates. This endpoint does, with the same
/// calculator the moderation commands use.</para>
///
/// <para>It must be safe to run twice: an operator re-running a deploy step must not change anything.</para>
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class ProviderRatingBackfillTests : ServiceCatalogIntegrationTestBase
{
    private const string Endpoint = "/api/v1/admin/reviews/recompute-ratings";

    public ProviderRatingBackfillTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private void AuthenticateWithSingleRole(string role) => AuthenticateAs(new TestUser
    {
        UserId = Guid.NewGuid().ToString(),
        Email = $"{role.ToLowerInvariant()}@nahalkmi.ir",
        Name = role,
        Role = role,
    });

    /// <summary>A provider whose reviews are published but whose stored rating was never written — production today.</summary>
    private async Task<Guid> StaleProviderAsync(params decimal[] publishedRatings)
    {
        var provider = await CreateAndAuthenticateAsProviderAsync($"stale {Guid.NewGuid():N}"[..20], $"{Guid.NewGuid():N}@test.com");
        ClearAuthenticationHeader();
        foreach (var rating in publishedRatings)
        {
            var review = Review.Create(provider.Id, UserId.From(Guid.NewGuid()), Guid.NewGuid(), rating);
            review.Publish("migration:AddReviewModerationAndVoting");
            await CreateEntityAsync(review);
        }
        return provider.Id.Value;
    }

    [Fact]
    public async Task A_production_admin_can_run_it_and_stale_ratings_become_true()
    {
        var providerId = await StaleProviderAsync(4.0m, 5.0m);
        (await FindProviderAsync(providerId))!.PublishedReviewCount.Should().Be(0, "the precondition: never written");

        AuthenticateWithSingleRole("Admin");
        var response = await Client.PostAsync(Endpoint, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var provider = await FindProviderAsync(providerId);
        provider!.PublishedReviewCount.Should().Be(2);
        provider.AverageRating.Should().Be(4.5m);
    }

    [Fact]
    public async Task Running_it_twice_changes_nothing_the_second_time()
    {
        var providerId = await StaleProviderAsync(3.0m, 4.0m, 5.0m);
        AuthenticateWithSingleRole("Admin");

        (await Client.PostAsync(Endpoint, null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var first = await FindProviderAsync(providerId);
        (await Client.PostAsync(Endpoint, null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await FindProviderAsync(providerId);

        second!.AverageRating.Should().Be(first!.AverageRating).And.Be(4.0m);
        second.PublishedReviewCount.Should().Be(first.PublishedReviewCount).And.Be(3);
    }

    [Fact]
    public async Task An_admin_holding_only_the_Administrator_spelling_is_let_in()
    {
        AuthenticateWithSingleRole("Administrator");

        var response = await Client.PostAsync(Endpoint, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_provider_cannot_run_it()
    {
        AuthenticateWithSingleRole("Provider");

        var response = await Client.PostAsync(Endpoint, null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_run_it()
    {
        ClearAuthenticationHeader();

        var response = await Client.PostAsync(Endpoint, null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
