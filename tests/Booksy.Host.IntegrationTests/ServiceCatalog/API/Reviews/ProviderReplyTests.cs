using System.Net;
using System.Net.Http.Json;
using Booksy.ServiceCatalog.Domain.Enums;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Reviews;

/// <summary>
/// The provider's reply (provider-reviews "The provider may reply to a review about them"; review-moderation
/// "A provider reply is moderated before it is public", "An edited review returns to the queue").
/// </summary>
/// <remarks>
/// Only the business may reply — administrators explicitly may not (design D11): a reply is published speech
/// attributed to the salon, and an admin writing it would be putting words in the salon's mouth.
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class ProviderReplyTests : ReviewTestBase
{
    public ProviderReplyTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private const string ReplyText = "ممنون از لطف شما، منتظر دیدارتان هستیم";

    private async Task ModerateAsync(Guid reviewId, string action, object? body = null)
    {
        AsAdmin();
        var response = await Client.PostAsJsonAsync($"/api/v1/admin/reviews/{reviewId}/{action}", body ?? new { });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        ClearAuthenticationHeader();
    }

    private async Task<(Guid ReviewId, Visit Visit)> PublishedReviewAsync()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 4.0m);
        await ModerateAsync(reviewId, "approve");
        return (reviewId, visit);
    }

    private Task<HttpResponseMessage> ReplyAsync(Guid reviewId, string text = ReplyText) =>
        Client.PostAsJsonAsync($"/api/v1/reviews/{reviewId}/reply", new { text });

    // ── The owning provider ──

    [Fact]
    public async Task The_owning_provider_replies_and_it_waits_for_a_moderator()
    {
        var (reviewId, visit) = await PublishedReviewAsync();
        AuthenticateAsProviderOwner(visit.Provider);

        var response = await ReplyAsync(reviewId);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var review = await LoadReviewAsync(reviewId);
        review.ProviderResponse.Should().Be(ReplyText);
        review.ReplyModerationStatus.Should().Be(ReviewModerationStatus.Pending);
        review.ModerationStatus.Should().Be(ReviewModerationStatus.Published, "the review itself stays public");
    }

    [Fact]
    public async Task A_pending_reply_is_in_the_queue_and_approving_it_publishes_it()
    {
        var (reviewId, visit) = await PublishedReviewAsync();
        AuthenticateAsProviderOwner(visit.Provider);
        (await ReplyAsync(reviewId)).StatusCode.Should().Be(HttpStatusCode.OK);

        AsAdmin();
        var queue = (JArray)Data(await (await Client.GetAsync("/api/v1/admin/reviews/queue")).Content.ReadAsStringAsync())["items"]!;
        var item = queue.Single(i => i["reviewId"]!.Value<string>() == reviewId.ToString());
        item["replyPending"]!.Value<bool>().Should().BeTrue();
        item["reviewPending"]!.Value<bool>().Should().BeFalse();
        item["providerResponse"]!.Value<string>().Should().Be(ReplyText);

        await ModerateAsync(reviewId, "reply/approve");

        (await LoadReviewAsync(reviewId)).ReplyModerationStatus.Should().Be(ReviewModerationStatus.Published);
    }

    [Fact]
    public async Task A_rejected_reply_leaves_the_review_published()
    {
        var (reviewId, visit) = await PublishedReviewAsync();
        AuthenticateAsProviderOwner(visit.Provider);
        (await ReplyAsync(reviewId, "با تخفیف ۵۰٪ دوباره بیایید، کد: SALE50")).StatusCode.Should().Be(HttpStatusCode.OK);

        await ModerateAsync(reviewId, "reply/reject", new { reason = "contains a discount code" });

        var review = await LoadReviewAsync(reviewId);
        review.ReplyModerationStatus.Should().Be(ReviewModerationStatus.Rejected);
        review.ReplyModerationReason.Should().Be("contains a discount code");
        review.ModerationStatus.Should().Be(ReviewModerationStatus.Published);
    }

    [Fact]
    public async Task A_second_reply_is_a_conflict_pointing_at_edit()
    {
        var (reviewId, visit) = await PublishedReviewAsync();
        AuthenticateAsProviderOwner(visit.Provider);
        await ReplyAsync(reviewId);

        var response = await ReplyAsync(reviewId, "یک پاسخ دیگر");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await LoadReviewAsync(reviewId)).ProviderResponse.Should().Be(ReplyText);
    }

    [Fact]
    public async Task Editing_a_published_reply_sends_it_back_to_the_queue()
    {
        var (reviewId, visit) = await PublishedReviewAsync();
        AuthenticateAsProviderOwner(visit.Provider);
        await ReplyAsync(reviewId);
        await ModerateAsync(reviewId, "reply/approve");

        AuthenticateAsProviderOwner(visit.Provider);
        var response = await Client.PutAsJsonAsync($"/api/v1/reviews/{reviewId}/reply", new { text = "ممنون، به‌زودی می‌بینیمتان" });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var review = await LoadReviewAsync(reviewId);
        review.ProviderResponse.Should().Be("ممنون، به‌زودی می‌بینیمتان");
        review.ReplyModerationStatus.Should().Be(ReviewModerationStatus.Pending);
    }

    [Fact]
    public async Task Removing_a_reply_clears_it()
    {
        var (reviewId, visit) = await PublishedReviewAsync();
        AuthenticateAsProviderOwner(visit.Provider);
        await ReplyAsync(reviewId);

        var response = await Client.DeleteAsync($"/api/v1/reviews/{reviewId}/reply");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = await LoadReviewAsync(reviewId);
        review.ProviderResponse.Should().BeNull();
        review.ReplyModerationStatus.Should().BeNull();
    }

    [Fact]
    public async Task Editing_a_review_that_carries_a_published_reply_takes_the_reply_back_to_pending()
    {
        // The provider's published words must never sit under text they never saw.
        var (reviewId, visit) = await PublishedReviewAsync();
        AuthenticateAsProviderOwner(visit.Provider);
        await ReplyAsync(reviewId);
        await ModerateAsync(reviewId, "reply/approve");

        AsCustomer(visit.CustomerId);
        (await Client.PutAsJsonAsync($"/api/v1/reviews/{reviewId}", new { rating = 1.0m, comment = "اصلاً راضی نبودم از برخوردشان" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await LoadReviewAsync(reviewId)).ReplyModerationStatus.Should().Be(ReviewModerationStatus.Pending);
    }

    // ── Everyone else ──

    [Fact]
    public async Task A_different_provider_cannot_reply()
    {
        var (reviewId, _) = await PublishedReviewAsync();
        var other = await CreateAndAuthenticateAsProviderAsync("Another Salon", $"{Guid.NewGuid():N}@test.com");
        AuthenticateAsProviderOwner(other);

        var response = await ReplyAsync(reviewId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await LoadReviewAsync(reviewId)).ProviderResponse.Should().BeNull();
    }

    [Fact]
    public async Task The_reviews_author_cannot_reply()
    {
        var (reviewId, visit) = await PublishedReviewAsync();
        AsCustomer(visit.CustomerId);

        var response = await ReplyAsync(reviewId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_administrator_cannot_reply_on_the_salons_behalf()
    {
        var (reviewId, _) = await PublishedReviewAsync();
        AsAdmin();

        var response = await ReplyAsync(reviewId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_review_still_awaiting_approval_cannot_be_replied_to()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit);
        AuthenticateAsProviderOwner(visit.Provider);

        var response = await ReplyAsync(reviewId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
