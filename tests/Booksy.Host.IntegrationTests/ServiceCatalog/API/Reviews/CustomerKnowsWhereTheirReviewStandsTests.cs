using System.Net;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Reviews;

/// <summary>
/// The customer's own booking says where its review stands, so the apps can offer «ثبت نظر», say why not yet, or show
/// the review already written (openspec/changes/_inline/customer-reviews-and-nahal-seed). Nothing said so before: the
/// app offered the button on status alone, forgot it had been used as soon as the page closed, and a past booking the
/// salon never marked done simply had no button and no reason.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class CustomerKnowsWhereTheirReviewStandsTests : ReviewTestBase
{
    public CustomerKnowsWhereTheirReviewStandsTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private static bool IsNull(JToken? token) => token is null || token.Type == JTokenType.Null;

    [Fact]
    public async Task A_completed_booking_without_a_review_can_be_reviewed()
    {
        var visit = await CompletedVisitAsync();

        var row = await MyBookingRowAsync(visit.CustomerId, visit.BookingId);

        row["canReview"]!.Value<bool>().Should().BeTrue();
        IsNull(row["reviewId"]).Should().BeTrue();
        IsNull(row["reviewBlockedReason"]).Should().BeTrue();
    }

    [Fact]
    public async Task The_booking_page_says_the_same()
    {
        var visit = await CompletedVisitAsync();

        var response = await BookingPageAsync(visit.CustomerId, visit.BookingId);

        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        Data(text)["canReview"]!.Value<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task A_reviewed_booking_carries_its_review_and_is_not_offered_again()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit);

        var row = await MyBookingRowAsync(visit.CustomerId, visit.BookingId);

        row["canReview"]!.Value<bool>().Should().BeFalse();
        row["reviewId"]!.Value<string>().Should().Be(reviewId.ToString());
        row["reviewStatus"]!.Value<string>().Should().Be("Pending", "it waits for a moderator, and the app says so");

        var page = Data(await (await BookingPageAsync(visit.CustomerId, visit.BookingId)).Content.ReadAsStringAsync());
        page["canReview"]!.Value<bool>().Should().BeFalse();
        page["reviewId"]!.Value<string>().Should().Be(reviewId.ToString());
    }

    [Fact]
    public async Task A_past_booking_the_salon_has_not_marked_done_says_why_it_cannot_be_reviewed_yet()
    {
        var visit = await PastUncompletedVisitAsync();

        var row = await MyBookingRowAsync(visit.CustomerId, visit.BookingId);

        row["canReview"]!.Value<bool>().Should().BeFalse();
        row["reviewBlockedReason"]!.Value<string>().Should().Contain("سالن").And.Contain("انجام‌شده");
    }

    [Fact]
    public async Task An_upcoming_booking_is_neither_reviewable_nor_explained()
    {
        var visit = await UpcomingVisitAsync();

        var row = await MyBookingRowAsync(visit.CustomerId, visit.BookingId);

        row["canReview"]!.Value<bool>().Should().BeFalse();
        IsNull(row["reviewBlockedReason"]).Should().BeTrue("there is nothing to review before the visit");
    }
}
