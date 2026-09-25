using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Reviews;

/// <summary>
/// Who sees which reviews (review-moderation: "not publicly visible until an administrator approves it", "The author
/// can list their own reviews across all states", "The owning provider can list reviews of their business across
/// all states", "Public review statistics are computed over published reviews only"; review-engagement "A reader
/// is told their own current vote").
/// </summary>
/// <remarks>
/// Three surfaces, deliberately distinct: the anonymous public listing (published only, to everyone — including the
/// owner's own token), the author's own list, and the owner-scoped business inbox.
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class ReviewListingTests : ReviewTestBase
{
    public ReviewListingTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    private async Task ModerateAsync(Guid reviewId, string action, object? body = null)
    {
        AsAdmin();
        var response = await Client.PostAsJsonAsync($"/api/v1/admin/reviews/{reviewId}/{action}", body ?? new { });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        ClearAuthenticationHeader();
    }

    /// <summary>One provider holding a review in each state. Returns the ids and the shared provider's visit.</summary>
    private async Task<(Visit First, Guid Published, Guid Pending, Guid Rejected, Guid Hidden)> OneOfEachAsync()
    {
        var first = await CompletedVisitAsync();
        var published = await ReviewedAsync(first, 5.0m, new
        {
            rating = 5.0m, comment = "خیلی تمیز و وقت‌شناس بودند", punctualityRating = 4.0m,
        });
        await ModerateAsync(published, "approve");

        var pending = await ReviewedAsync(await CompletedVisitAsync(first.Provider), 1.0m);

        var rejected = await ReviewedAsync(await CompletedVisitAsync(first.Provider), 1.0m);
        await ModerateAsync(rejected, "reject", new { reason = "contains a phone number" });

        var hidden = await ReviewedAsync(await CompletedVisitAsync(first.Provider), 1.0m);
        await ModerateAsync(hidden, "approve");
        await ModerateAsync(hidden, "hide", new { reason = "reported and upheld" });

        return (first, published, pending, rejected, hidden);
    }

    private async Task<JToken> PublicListingAsync(Visit visit)
    {
        var response = await Client.GetAsync($"/api/v1/reviews/providers/{visit.Provider.Id.Value}?pageSize=50");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return Data(body);
    }

    private static List<Guid> Ids(JToken listing) =>
        ((JArray)listing["reviews"]!["items"]!).Select(i => Guid.Parse(i["reviewId"]!.Value<string>()!)).ToList();

    // ── The public listing ──

    [Fact]
    public async Task The_public_listing_returns_published_reviews_only()
    {
        var all = await OneOfEachAsync();

        var listing = await PublicListingAsync(all.First);

        Ids(listing).Should().Equal(all.Published);
        listing["reviews"]!["totalCount"]!.Value<int>().Should().Be(1);
    }

    [Fact]
    public async Task The_owning_providers_own_token_gets_nothing_extra_from_the_public_listing()
    {
        var all = await OneOfEachAsync();
        AuthenticateAsProviderOwner(all.First.Provider);

        Ids(await PublicListingAsync(all.First)).Should().Equal(all.Published);
    }

    [Fact]
    public async Task Public_statistics_are_computed_over_published_reviews_only()
    {
        // Before this change the statistics block averaged every review in every state.
        var all = await OneOfEachAsync();

        var stats = (await PublicListingAsync(all.First))["statistics"]!;

        stats["totalReviews"]!.Value<int>().Should().Be(1);
        stats["averageRating"]!.Value<decimal>().Should().Be(5.0m);
        stats["verifiedReviews"]!.Value<int>().Should().Be(1);
        stats["ratingDistribution"]!["oneStarCount"]!.Value<int>().Should().Be(0);
        stats["ratingDistribution"]!["fiveStarCount"]!.Value<int>().Should().Be(1);
    }

    [Fact]
    public async Task Public_statistics_carry_the_dimension_averages()
    {
        var all = await OneOfEachAsync();

        var stats = (await PublicListingAsync(all.First))["statistics"]!;

        stats["punctuality"]!["average"]!.Value<decimal>().Should().Be(4.0m);
        stats["punctuality"]!["count"]!.Value<int>().Should().Be(1);
        stats["cleanliness"]!["count"]!.Value<int>().Should().Be(0);
    }

    [Fact]
    public async Task A_listed_review_carries_its_dimensions()
    {
        var all = await OneOfEachAsync();

        var item = (await PublicListingAsync(all.First))["reviews"]!["items"]![0]!;

        item["punctualityRating"]!.Value<decimal>().Should().Be(4.0m);
    }

    [Fact]
    public async Task A_reply_is_shown_publicly_only_once_approved()
    {
        var all = await OneOfEachAsync();
        AuthenticateAsProviderOwner(all.First.Provider);
        (await Client.PostAsJsonAsync($"/api/v1/reviews/{all.Published}/reply", new { text = "ممنون از شما" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        ClearAuthenticationHeader();

        var beforeApproval = (await PublicListingAsync(all.First))["reviews"]!["items"]![0]!;
        await ModerateAsync(all.Published, "reply/approve");
        var afterApproval = (await PublicListingAsync(all.First))["reviews"]!["items"]![0]!;

        beforeApproval["providerResponse"]?.Value<string>().Should().BeNull("a pending reply is not public");
        afterApproval["providerResponse"]!.Value<string>().Should().Be("ممنون از شما");
    }

    [Fact]
    public async Task A_signed_in_reader_is_told_their_own_vote_and_an_anonymous_one_is_not()
    {
        var all = await OneOfEachAsync();
        var reader = Guid.NewGuid();
        AsCustomer(reader);
        (await Client.PutAsJsonAsync($"/api/v1/reviews/{all.Published}/helpful", new { isHelpful = true }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        AsCustomer(reader);
        var mine = (await PublicListingAsync(all.First))["reviews"]!["items"]![0]!;
        // Spec "Reader who has not voted" is an AUTHENTICATED reader: someone else's vote must never be reported as theirs.
        AsCustomer(Guid.NewGuid());
        var signedInNonVoter = (await PublicListingAsync(all.First))["reviews"]!["items"]![0]!;
        ClearAuthenticationHeader();
        var anonymous = (await PublicListingAsync(all.First))["reviews"]!["items"]![0]!;

        mine["myVote"]!.Value<string>().Should().Be("helpful");
        mine["helpfulCount"]!.Value<int>().Should().Be(1);
        signedInNonVoter["myVote"]?.Value<string>().Should().BeNull();
        signedInNonVoter["helpfulCount"]!.Value<int>().Should().Be(1);
        anonymous["myVote"]?.Value<string>().Should().BeNull();
        anonymous["helpfulCount"]!.Value<int>().Should().Be(1);
    }

    // ── The author's own list ──

    [Fact]
    public async Task The_author_sees_their_reviews_in_every_state_with_the_reason()
    {
        var visit = await CompletedVisitAsync();
        var mineRejected = await ReviewedAsync(visit, 1.0m);
        await ModerateAsync(mineRejected, "reject", new { reason = "contains a phone number" });
        await ReviewedAsync(await CompletedVisitAsync(), 3.0m); // someone else's review

        AsCustomer(visit.CustomerId);
        var response = await Client.GetAsync("/api/v1/reviews/me");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var items = (JArray)Data(body)["items"]!;
        var item = items.Single();
        item["reviewId"]!.Value<string>().Should().Be(mineRejected.ToString());
        item["moderationStatus"]!.Value<string>().Should().Be("Rejected");
        item["moderationReason"]!.Value<string>().Should().Be("contains a phone number");
        item["canEdit"]!.Value<bool>().Should().BeFalse("a rejected review is not editable");
    }

    [Fact]
    public async Task The_authors_list_names_the_salon_and_the_service_they_reviewed()
    {
        // customer-profile "Review Management": each entry shows the provider's name and the service.
        var visit = await CompletedVisitAsync();
        await ReviewedAsync(visit, 4.0m);
        var service = (await GetProviderServicesAsync(visit.Provider.Id.Value)).First();

        AsCustomer(visit.CustomerId);
        var item = ((JArray)Data(await (await Client.GetAsync("/api/v1/reviews/me")).Content.ReadAsStringAsync())["items"]!).Single();

        item["providerName"]!.Value<string>().Should().Be(visit.Provider.Profile.BusinessName);
        item["serviceName"]!.Value<string>().Should().Be(service.Name);
    }

    [Fact]
    public async Task The_author_sees_a_pending_review_marked_as_awaiting_approval()
    {
        var visit = await CompletedVisitAsync();
        await ReviewedAsync(visit, 4.0m);

        AsCustomer(visit.CustomerId);
        var item = ((JArray)Data(await (await Client.GetAsync("/api/v1/reviews/me")).Content.ReadAsStringAsync())["items"]!).Single();

        item["moderationStatus"]!.Value<string>().Should().Be("Pending");
        item["canEdit"]!.Value<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task An_anonymous_caller_has_no_own_list()
    {
        var response = await Client.GetAsync("/api/v1/reviews/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── The owner's inbox ──

    [Fact]
    public async Task The_owning_provider_sees_every_state_in_their_inbox()
    {
        var all = await OneOfEachAsync();
        AuthenticateAsProviderOwner(all.First.Provider);

        var response = await Client.GetAsync($"/api/v1/reviews/providers/{all.First.Provider.Id.Value}/inbox?pageSize=50");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var items = (JArray)Data(body)["items"]!;
        items.Select(i => Guid.Parse(i["reviewId"]!.Value<string>()!))
            .Should().BeEquivalentTo(new[] { all.Published, all.Pending, all.Rejected, all.Hidden });
        items.Single(i => i["reviewId"]!.Value<string>() == all.Pending.ToString())["moderationStatus"]!
            .Value<string>().Should().Be("Pending");
    }

    [Fact]
    public async Task The_inbox_counts_the_published_reviews_still_awaiting_the_business_reply()
    {
        // Awaiting = public and unanswered, or answered with a reply an administrator refused (the business has to
        // rephrase). A reply awaiting approval is the business's part done; pending/rejected/hidden reviews cannot
        // be replied to at all. Counted over every review, not the page — the Home badge must not depend on paging.
        var all = await OneOfEachAsync();

        var answered = await ReviewedAsync(await CompletedVisitAsync(all.First.Provider), 4.0m);
        await ModerateAsync(answered, "approve");
        var refusedReply = await ReviewedAsync(await CompletedVisitAsync(all.First.Provider), 4.0m);
        await ModerateAsync(refusedReply, "approve");

        AuthenticateAsProviderOwner(all.First.Provider);
        foreach (var id in new[] { answered, refusedReply })
        {
            var reply = await Client.PostAsJsonAsync($"/api/v1/reviews/{id}/reply", new { text = "ممنون از لطف شما" });
            reply.StatusCode.Should().Be(HttpStatusCode.OK, await reply.Content.ReadAsStringAsync());
        }
        await ModerateAsync(refusedReply, "reply/reject", new { reason = "contains a phone number" });

        AuthenticateAsProviderOwner(all.First.Provider);
        var response = await Client.GetAsync($"/api/v1/reviews/providers/{all.First.Provider.Id.Value}/inbox?pageSize=1");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        Data(body)["awaitingReplyCount"]!.Value<int>().Should().Be(2, "the unanswered one and the refused reply");
    }

    [Fact]
    public async Task Another_provider_cannot_open_the_inbox()
    {
        var all = await OneOfEachAsync();
        var other = await CreateAndAuthenticateAsProviderAsync("Another Salon", $"{Guid.NewGuid():N}@test.com");
        AuthenticateAsProviderOwner(other);

        var response = await Client.GetAsync($"/api/v1/reviews/providers/{all.First.Provider.Id.Value}/inbox");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
