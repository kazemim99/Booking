using System.Net;
using System.Net.Http.Json;
using Booksy.ServiceCatalog.Domain.Enums;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Reviews;

/// <summary>
/// The moderation endpoints, and the provider rating they keep true (review-moderation; provider-reviews
/// "Provider rating aggregates derive from published reviews only"; tasks 4.3/4.4, 5.11–5.13, 6.5).
/// </summary>
/// <remarks>
/// Every rating assertion reads the provider through a FRESH scope after the response. The rating is recomputed
/// inline in the moderation command, inside its transaction; a test that raised an event against an
/// already-committed row, or read a tracked entity, would pass even if that recompute ran one action stale.
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class ReviewModerationTests : ReviewTestBase
{
    public ReviewModerationTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private Task<HttpResponseMessage> PostAsync(string path, object? body = null) =>
        Client.PostAsJsonAsync($"/api/v1/admin/reviews/{path}", body ?? new { });

    private async Task ModerateAsync(Guid reviewId, string action, object? body = null)
    {
        AsAdmin();
        var response = await PostAsync($"{reviewId}/{action}", body);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        ClearAuthenticationHeader();
    }

    private async Task<(Guid ReviewId, Visit Visit)> PublishedAsync(decimal rating, Visit? sameProviderAs = null)
    {
        var visit = await CompletedVisitAsync(sameProviderAs?.Provider);
        var reviewId = await ReviewedAsync(visit, rating);
        await ModerateAsync(reviewId, "approve");
        return (reviewId, visit);
    }

    private async Task<(decimal Average, int Count)> RatingOfAsync(Visit visit)
    {
        var provider = await LoadProviderAsync(visit.Provider.Id);
        return (provider.AverageRating, provider.PublishedReviewCount);
    }

    // ── Approve / reject ──

    [Fact]
    public async Task Approving_publishes_the_review_and_the_providers_rating_moves()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 4.0m);

        await ModerateAsync(reviewId, "approve");

        (await LoadReviewAsync(reviewId)).ModerationStatus.Should().Be(ReviewModerationStatus.Published);
        (await RatingOfAsync(visit)).Should().Be((4.0m, 1));
    }

    [Fact]
    public async Task A_second_approval_of_the_same_provider_averages_both()
    {
        var (_, first) = await PublishedAsync(4.0m);
        await PublishedAsync(5.0m, sameProviderAs: first);

        (await RatingOfAsync(first)).Should().Be((4.5m, 2));
    }

    [Fact]
    public async Task Rejecting_keeps_the_review_out_of_public_view_and_out_of_the_rating()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 1.0m);

        await ModerateAsync(reviewId, "reject", new { reason = "contains a phone number" });

        var review = await LoadReviewAsync(reviewId);
        review.ModerationStatus.Should().Be(ReviewModerationStatus.Rejected);
        review.ModerationReason.Should().Be("contains a phone number");
        (await RatingOfAsync(visit)).Should().Be((0m, 0));
    }

    [Fact]
    public async Task Rejecting_without_a_reason_is_refused()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit);
        AsAdmin();

        var response = await PostAsync($"{reviewId}/reject", new { reason = " " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LoadReviewAsync(reviewId)).ModerationStatus.Should().Be(ReviewModerationStatus.Pending);
    }

    [Fact]
    public async Task Approving_twice_is_refused_rather_than_silently_repeated()
    {
        var (reviewId, _) = await PublishedAsync(4.0m);
        AsAdmin();

        var response = await PostAsync($"{reviewId}/approve");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Moderating_a_review_that_does_not_exist_is_not_found()
    {
        AsAdmin();

        var response = await PostAsync($"{Guid.NewGuid()}/approve");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Hide / restore ──

    [Fact]
    public async Task Hiding_takes_the_review_down_and_the_rating_is_recomputed_without_it()
    {
        var (hidden, visit) = await PublishedAsync(1.0m);
        await PublishedAsync(5.0m, sameProviderAs: visit);

        await ModerateAsync(hidden, "hide", new { reason = "reported and upheld" });

        var review = await LoadReviewAsync(hidden);
        review.ModerationStatus.Should().Be(ReviewModerationStatus.Hidden);
        review.IsVerified.Should().BeTrue("moderation never touches verification");
        (await RatingOfAsync(visit)).Should().Be((5.0m, 1));
    }

    [Fact]
    public async Task Restoring_a_hidden_review_brings_it_back_into_the_rating()
    {
        var (reviewId, visit) = await PublishedAsync(2.0m);
        await PublishedAsync(4.0m, sameProviderAs: visit);
        await ModerateAsync(reviewId, "hide", new { reason = "reported" });

        await ModerateAsync(reviewId, "restore");

        (await LoadReviewAsync(reviewId)).ModerationStatus.Should().Be(ReviewModerationStatus.Published);
        (await RatingOfAsync(visit)).Should().Be((3.0m, 2));
    }

    [Fact]
    public async Task A_rejected_review_cannot_be_restored()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit);
        await ModerateAsync(reviewId, "reject", new { reason = "spam" });
        AsAdmin();

        var response = await PostAsync($"{reviewId}/restore");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LoadReviewAsync(reviewId)).ModerationStatus.Should().Be(ReviewModerationStatus.Rejected);
    }

    // ── The queue ──

    private async Task<JArray> QueueAsync(string filter = "pending")
    {
        AsAdmin();
        var response = await Client.GetAsync($"/api/v1/admin/reviews/queue?filter={filter}&pageSize=50");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        ClearAuthenticationHeader();
        return (JArray)Data(body)["items"]!;
    }

    [Fact]
    public async Task The_queue_lists_pending_reviews_oldest_first_with_what_a_moderator_needs()
    {
        var older = await ReviewedAsync(await CompletedVisitAsync(), 3.0m);
        var newer = await ReviewedAsync(await CompletedVisitAsync(), 5.0m);
        var (published, _) = await PublishedAsync(4.0m);

        var items = await QueueAsync();

        items.Select(i => Guid.Parse(i["reviewId"]!.Value<string>()!)).Should().Equal(older, newer);
        items.Should().NotContain(i => i["reviewId"]!.Value<string>() == published.ToString());
        var first = items[0];
        first["rating"]!.Value<decimal>().Should().Be(3.0m);
        first["comment"]!.Value<string>().Should().NotBeNullOrEmpty();
        first["providerId"].Should().NotBeNull();
        first["customerId"].Should().NotBeNull();
        first["reviewPending"]!.Value<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task Hidden_reviews_are_found_on_their_own_filter_with_the_reason()
    {
        var (reviewId, _) = await PublishedAsync(2.0m);
        await ModerateAsync(reviewId, "hide", new { reason = "reported and upheld" });

        var hidden = await QueueAsync("hidden");
        var pending = await QueueAsync("pending");

        var item = hidden.Single(i => i["reviewId"]!.Value<string>() == reviewId.ToString());
        item["moderationReason"]!.Value<string>().Should().Be("reported and upheld");
        pending.Should().NotContain(i => i["reviewId"]!.Value<string>() == reviewId.ToString());
    }

    // ── Who may moderate ──

    [Theory]
    [InlineData("Admin")]
    [InlineData("Administrator")]
    public async Task Every_admin_role_spelling_can_work_the_queue(string role)
    {
        // Not the shared TestUser.Admin: it carries all three spellings and would pass against any of them,
        // which is exactly how the 2026-09-19 403 incident escaped.
        AsAdmin(role);

        var response = await Client.GetAsync("/api/v1/admin/reviews/queue");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_owning_provider_cannot_moderate_a_review_of_their_own_business()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 1.0m);
        AuthenticateAsProviderOwner(visit.Provider);

        var approve = await PostAsync($"{reviewId}/approve");
        var queue = await Client.GetAsync("/api/v1/admin/reviews/queue");

        approve.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        queue.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await LoadReviewAsync(reviewId)).ModerationStatus.Should().Be(ReviewModerationStatus.Pending);
    }

    [Fact]
    public async Task A_customer_cannot_see_the_queue()
    {
        AsCustomer(Guid.NewGuid());

        var response = await Client.GetAsync("/api/v1/admin/reviews/queue");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
