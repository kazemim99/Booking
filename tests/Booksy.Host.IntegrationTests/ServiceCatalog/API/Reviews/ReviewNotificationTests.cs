using System.Net;
using System.Net.Http.Json;
using Booksy.ServiceCatalog.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Reviews;

/// <summary>
/// The notifications a review causes (tasks 7.1, 7.6). Raised inline by the moderation command at the moment
/// something becomes public — never at submission — and addressed through the booking the review is about.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class ReviewNotificationTests : ReviewTestBase
{
    public ReviewNotificationTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private sealed record Raised(NotificationEventCode Code, Guid RecipientId, string? SubjectType, Guid? SubjectId, string Parameters);

    private Task<List<Raised>> RaisedForAsync(Guid bookingId, params NotificationEventCode[] codes) =>
        FreshAsync(db => db.NotificationOutbox.AsNoTracking()
            .Where(e => e.SubjectId == bookingId && codes.Contains(e.EventCode))
            .OrderBy(e => e.CreatedAt)
            .Select(e => new Raised(e.EventCode, e.RecipientId, e.SubjectType, e.SubjectId, e.ParametersJson))
            .ToListAsync());

    private static readonly NotificationEventCode[] ReviewCodes =
    {
        NotificationEventCode.ReviewPublished, NotificationEventCode.ReviewRepublished,
        NotificationEventCode.ReviewReplyPublished, NotificationEventCode.ReviewRejected,
    };

    private async Task ModerateAsync(Guid reviewId, string action, object? body = null)
    {
        AsAdmin();
        var response = await Client.PostAsJsonAsync($"/api/v1/admin/reviews/{reviewId}/{action}", body ?? new { });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        ClearAuthenticationHeader();
    }

    [Fact]
    public async Task Submitting_a_review_notifies_nobody_yet()
    {
        var visit = await CompletedVisitAsync();

        await ReviewedAsync(visit, 4.5m);

        (await RaisedForAsync(visit.BookingId, ReviewCodes)).Should().BeEmpty(
            "a review waiting for a moderator is not something the salon can see publicly");
    }

    [Fact]
    public async Task Publishing_a_review_tells_the_salons_owner_with_the_rating_through_the_booking()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 4.5m);

        await ModerateAsync(reviewId, "approve");

        var raised = (await RaisedForAsync(visit.BookingId, ReviewCodes)).Single();
        raised.Code.Should().Be(NotificationEventCode.ReviewPublished);
        raised.RecipientId.Should().Be(visit.Provider.OwnerId.Value, "notifications are addressed to the owner's user id");
        raised.SubjectType.Should().Be("Booking");
        raised.SubjectId.Should().Be(visit.BookingId);
        JObject.Parse(raised.Parameters)["rating"]!.Value<string>().Should().Be("4.5");
    }

    [Fact]
    public async Task Approving_an_edited_review_is_announced_as_a_change_not_as_a_new_review()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 5.0m);
        await ModerateAsync(reviewId, "approve");
        AsCustomer(visit.CustomerId);
        (await Client.PutAsJsonAsync($"/api/v1/reviews/{reviewId}", new { rating = 2.0m }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        await ModerateAsync(reviewId, "approve");

        (await RaisedForAsync(visit.BookingId, ReviewCodes)).Select(r => r.Code)
            .Should().Equal(NotificationEventCode.ReviewPublished, NotificationEventCode.ReviewRepublished);
    }

    [Fact]
    public async Task Restoring_a_hidden_review_is_announced_as_a_change()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 3.0m);
        await ModerateAsync(reviewId, "approve");
        await ModerateAsync(reviewId, "hide", new { reason = "reported" });

        await ModerateAsync(reviewId, "restore");

        (await RaisedForAsync(visit.BookingId, ReviewCodes)).Select(r => r.Code)
            .Should().Equal(NotificationEventCode.ReviewPublished, NotificationEventCode.ReviewRepublished);
    }

    [Fact]
    public async Task Rejecting_a_review_tells_its_author_why_and_tells_the_salon_nothing()
    {
        // Task 7.7, decided 2026-09-22 (this test previously asserted that nobody was told): a rejected review
        // used to vanish in silence, and the review request is withdrawn at submission, so nothing ever asked again.
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 1.0m);

        await ModerateAsync(reviewId, "reject", new { reason = "contains a phone number" });

        var raised = (await RaisedForAsync(visit.BookingId, ReviewCodes)).Single();
        raised.Code.Should().Be(NotificationEventCode.ReviewRejected);
        raised.RecipientId.Should().Be(visit.CustomerId, "the author, never the salon");
        raised.SubjectId.Should().Be(visit.BookingId);
        raised.Parameters.Should().Contain("contains a phone number", "the moderator's reason travels with it");
    }

    [Fact]
    public async Task Hiding_a_published_review_tells_its_author_nothing()
    {
        // Only rejection was decided (7.7). Hiding is usually the outcome of someone else's report and the review
        // can be restored, so it stays silent until that is decided on its own.
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 2.0m);
        await ModerateAsync(reviewId, "approve");

        await ModerateAsync(reviewId, "hide", new { reason = "reported and upheld" });

        (await RaisedForAsync(visit.BookingId, ReviewCodes)).Select(r => r.Code)
            .Should().Equal(NotificationEventCode.ReviewPublished);
    }

    [Fact]
    public async Task Publishing_the_salons_reply_tells_the_reviews_author()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 4.0m);
        await ModerateAsync(reviewId, "approve");
        AuthenticateAsProviderOwner(visit.Provider);
        (await Client.PostAsJsonAsync($"/api/v1/reviews/{reviewId}/reply", new { text = "ممنون از لطف شما" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        ClearAuthenticationHeader();

        await ModerateAsync(reviewId, "reply/approve");

        var reply = (await RaisedForAsync(visit.BookingId, NotificationEventCode.ReviewReplyPublished)).Single();
        reply.RecipientId.Should().Be(visit.CustomerId);
        reply.SubjectId.Should().Be(visit.BookingId);
    }

    [Fact]
    public async Task Restoring_a_review_does_not_re_announce_a_reply_the_customer_was_already_told_about()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 4.0m);
        await ModerateAsync(reviewId, "approve");
        AuthenticateAsProviderOwner(visit.Provider);
        (await Client.PostAsJsonAsync($"/api/v1/reviews/{reviewId}/reply", new { text = "ممنون از لطف شما" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        ClearAuthenticationHeader();
        await ModerateAsync(reviewId, "reply/approve");
        await ModerateAsync(reviewId, "hide", new { reason = "reported" });

        await ModerateAsync(reviewId, "restore");

        (await RaisedForAsync(visit.BookingId, NotificationEventCode.ReviewReplyPublished)).Should().ContainSingle();
    }

    [Fact]
    public async Task After_reviewing_the_customers_inbox_holds_no_review_request_for_that_booking()
    {
        // At the user-visible boundary, not the outbox table: a legacy second store once put a review request in
        // the inbox that outbox-only withdrawal could not reach (since removed by the notification-system work).
        var visit = await CompletedVisitAsync();
        await ReviewedAsync(visit, 5.0m);

        AsCustomer(visit.CustomerId);
        var response = await Client.GetAsync("/api/v1/notifications/inbox?pageSize=100");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        body.Should().NotContain("ReviewRequest").And.NotContain("ReviewReminder");
        var pendingAsks = await RaisedForAsync(visit.BookingId, NotificationEventCode.ReviewRequest, NotificationEventCode.ReviewReminder);
        pendingAsks.Should().NotBeEmpty("the ask was raised at completion");
        (await FreshAsync(db => db.NotificationOutbox.AsNoTracking()
                .Where(e => e.SubjectId == visit.BookingId
                            && (e.EventCode == NotificationEventCode.ReviewRequest || e.EventCode == NotificationEventCode.ReviewReminder))
                .Select(e => e.State).ToListAsync()))
            .Should().OnlyContain(state => state != "Pending", "submitting the review withdrew both asks");
    }
}
