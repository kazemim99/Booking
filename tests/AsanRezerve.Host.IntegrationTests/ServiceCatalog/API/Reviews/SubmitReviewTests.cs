using System.Net;
using System.Net.Http.Json;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Reviews;

/// <summary>
/// Submitting a review: who may, for what, and what state it lands in
/// (provider-reviews "Only the customer of a completed booking may review it", review-moderation "not publicly
/// visible until an administrator approves it").
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class SubmitReviewTests : ReviewTestBase
{
    public SubmitReviewTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task The_bookings_customer_can_review_it_and_it_waits_for_a_moderator()
    {
        var visit = await CompletedVisitAsync();

        var reviewId = await ReviewedAsync(visit, 4.5m);

        var review = await LoadReviewAsync(reviewId);
        review.ModerationStatus.Should().Be(ReviewModerationStatus.Pending);
        review.IsVerified.Should().BeTrue();
        review.BookingId.Should().Be(visit.BookingId);
        review.ProviderId.Should().Be(visit.Provider.Id);
    }

    [Fact]
    public async Task The_response_says_the_review_is_awaiting_approval()
    {
        var visit = await CompletedVisitAsync();

        var response = await SubmitAsync(visit, AReview());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Data(await response.Content.ReadAsStringAsync())["moderationStatus"]!.Value<string>().Should().Be("Pending");
    }

    [Fact]
    public async Task Dimensions_are_stored_and_the_ones_left_out_stay_absent()
    {
        var visit = await CompletedVisitAsync();

        var reviewId = await ReviewedAsync(visit, body: new
        {
            rating = 5.0m,
            comment = "بسیار تمیز بود ولی کمی دیر شروع شد",
            cleanlinessRating = 5.0m,
            punctualityRating = 3.0m,
        });

        var review = await LoadReviewAsync(reviewId);
        review.RatingValue.Should().Be(5.0m, "the overall is the customer's own verdict, never recomputed");
        review.CleanlinessRating.Should().Be(5.0m);
        review.PunctualityRating.Should().Be(3.0m);
        review.SkillRating.Should().BeNull();
        review.ConductRating.Should().BeNull();
    }

    [Fact]
    public async Task A_review_without_the_overall_rating_is_refused_naming_it()
    {
        var visit = await CompletedVisitAsync();

        var response = await SubmitAsync(visit, new { cleanlinessRating = 4.0m, comment = "کار تمیز و دقیقی بود" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Rating");
    }

    [Fact]
    public async Task An_off_increment_dimension_is_refused_naming_it()
    {
        var visit = await CompletedVisitAsync();

        var response = await SubmitAsync(visit, new { rating = 4.0m, skillRating = 3.7m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("SkillRating");
    }

    [Fact]
    public async Task Someone_who_did_not_make_the_booking_is_refused()
    {
        var visit = await CompletedVisitAsync();

        AsCustomer(Guid.NewGuid());
        var response = await Client.PostAsJsonAsync($"/api/v1/reviews/bookings/{visit.BookingId}", AReview());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_booking_that_has_not_been_completed_cannot_be_reviewed()
    {
        var visit = await UncompletedVisitAsync();

        var response = await SubmitAsync(visit, AReview());

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_second_review_of_the_same_booking_is_refused()
    {
        var visit = await CompletedVisitAsync();
        await ReviewedAsync(visit);

        var response = await SubmitAsync(visit, AReview(3.0m));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_review()
    {
        var visit = await CompletedVisitAsync();
        ClearAuthenticationHeader();

        var response = await Client.PostAsJsonAsync($"/api/v1/reviews/bookings/{visit.BookingId}", AReview());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_pending_review_does_not_move_the_providers_rating()
    {
        var visit = await CompletedVisitAsync();

        await ReviewedAsync(visit, 5.0m);

        var provider = await LoadProviderAsync(visit.Provider.Id);
        provider.PublishedReviewCount.Should().Be(0);
        provider.HasRating.Should().BeFalse();
    }
}
