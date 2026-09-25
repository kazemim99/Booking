using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Reviews;

/// <summary>
/// openspec/changes/_inline/reviews-and-reschedule-round2, tasks 1–3: the review form rates the four aspects and the
/// overall is their average to the half star (D1); the author chooses whether the public sees their name (D2); and a
/// customer reviews a salon once, from whichever visit — the others offer «ویرایش نظر» (D4).
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class OneReviewPerSalonAndAspectsTests : ReviewTestBase
{
    public OneReviewPerSalonAndAspectsTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    private static object Aspects(decimal c, decimal s, decimal p, decimal d, bool? showName = null) => showName is { } show
        ? new { cleanlinessRating = c, skillRating = s, punctualityRating = p, conductRating = d, showName = show, comment = "کار تمیز و دقیقی بود، ممنون از شما" }
        : new { cleanlinessRating = c, skillRating = s, punctualityRating = p, conductRating = d, comment = "کار تمیز و دقیقی بود، ممنون از شما" };

    private static bool IsNull(JToken? token) => token is null || token.Type == JTokenType.Null;

    private static JToken FirstError(string body) => JObject.Parse(body)["errors"]![0]!;

    // ── D1: the overall from the four aspects ──

    [Fact]
    public async Task Without_an_overall_it_is_the_four_aspects_average_to_the_half_star()
    {
        var visit = await CompletedVisitAsync();

        var response = await SubmitAsync(visit, Aspects(3.0m, 4.0m, 4.0m, 4.0m));

        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, text);
        Data(text)["rating"]!.Value<decimal>().Should().Be(4.0m, "3.75 rounds up to the next half star");
        var review = await LoadReviewAsync(Guid.Parse(Data(text)["reviewId"]!.Value<string>()!));
        review.RatingValue.Should().Be(4.0m);
        review.CleanlinessRating.Should().Be(3.0m);
        review.ConductRating.Should().Be(4.0m);
    }

    [Fact]
    public async Task Without_an_overall_a_missing_aspect_is_refused_as_the_rating_in_persian()
    {
        var visit = await CompletedVisitAsync();

        var response = await SubmitAsync(visit, new { cleanlinessRating = 4.0m, skillRating = 4.0m, punctualityRating = 4.0m });

        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, text);
        FirstError(text)["field"]!.Value<string>().Should().Be("Rating");
        FirstError(text)["message"]!.Value<string>().Should().Be("به هر چهار مورد امتیاز بدهید.");
    }

    [Fact]
    public async Task An_edit_without_an_overall_derives_it_too()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 5.0m);

        AsCustomer(visit.CustomerId);
        var response = await Client.PutAsJsonAsync($"/api/v1/reviews/{reviewId}", Aspects(3.0m, 3.0m, 3.0m, 4.0m));

        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        Data(text)["rating"]!.Value<decimal>().Should().Be(3.5m, "3.25 rounds up to the half star");
        (await LoadReviewAsync(reviewId)).RatingValue.Should().Be(3.5m);
    }

    // ── D2: the author's name choice ──

    [Fact]
    public async Task The_author_can_hide_their_name_and_sees_the_choice_on_their_own_review()
    {
        var visit = await CompletedVisitAsync();

        var response = await SubmitAsync(visit, Aspects(5.0m, 5.0m, 5.0m, 5.0m, showName: false));

        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, text);
        Data(text)["showName"]!.Value<bool>().Should().BeFalse();
        var reviewId = Guid.Parse(Data(text)["reviewId"]!.Value<string>()!);
        (await LoadReviewAsync(reviewId)).ShowName.Should().BeFalse();

        var mine = await MyReviewAsync(visit.CustomerId, reviewId);
        mine["showName"]!.Value<bool>().Should().BeFalse("the edit form opens with the author's choice");
    }

    [Fact]
    public async Task Leaving_the_choice_out_shows_the_name_and_an_edit_can_change_it()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit);
        (await LoadReviewAsync(reviewId)).ShowName.Should().BeTrue();

        AsCustomer(visit.CustomerId);
        var response = await Client.PutAsJsonAsync($"/api/v1/reviews/{reviewId}", Aspects(4.0m, 4.0m, 4.0m, 4.0m, showName: false));

        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        Data(text)["showName"]!.Value<bool>().Should().BeFalse();
        (await LoadReviewAsync(reviewId)).ShowName.Should().BeFalse();
    }

    // ── D4: one review per customer per salon ──

    [Fact]
    public async Task A_second_review_of_the_same_salon_from_another_visit_is_refused_naming_the_edit()
    {
        var first = await CompletedVisitAsync();
        await ReviewedAsync(first);
        var second = await CompletedVisitAsync(first.Provider, first.CustomerId);

        var response = await SubmitAsync(second, AReview(3.0m));

        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Conflict, text);
        FirstError(text)["message"]!.Value<string>().Should()
            .Be("برای این سالن قبلاً نظر داده‌اید؛ می‌توانید همان را از «نظرهای من» ویرایش کنید.");
    }

    [Fact]
    public async Task Another_salon_can_still_be_reviewed()
    {
        var first = await CompletedVisitAsync();
        await ReviewedAsync(first);
        var elsewhere = await CompletedVisitAsync(customer: first.CustomerId);

        (await SubmitAsync(elsewhere, AReview())).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Every_visit_to_a_reviewed_salon_carries_that_review_and_offers_its_edit()
    {
        var first = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(first);
        var second = await CompletedVisitAsync(first.Provider, first.CustomerId);

        var row = await MyBookingRowAsync(second.CustomerId, second.BookingId);
        var page = Data(await (await BookingPageAsync(second.CustomerId, second.BookingId)).Content.ReadAsStringAsync());

        foreach (var booking in new[] { row, page })
        {
            booking["canReview"]!.Value<bool>().Should().BeFalse("the salon is already reviewed");
            booking["reviewId"]!.Value<string>().Should().Be(reviewId.ToString());
            booking["reviewStatus"]!.Value<string>().Should().Be("Pending");
            booking["reviewEditable"]!.Value<bool>().Should().BeTrue("«ویرایش نظر» instead of «ثبت نظر»");
            booking["reviewBookingId"]!.Value<string>().Should().Be(first.BookingId.ToString(),
                "the review was written for the other visit");
        }

        var own = await MyBookingRowAsync(first.CustomerId, first.BookingId);
        own["reviewBookingId"]!.Value<string>().Should().Be(first.BookingId.ToString());
    }

    [Fact]
    public async Task Past_the_edit_window_the_review_is_still_carried_but_no_longer_editable()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit);
        await FreshAsync(async db =>
        {
            var review = await db.Reviews.FindAsync(reviewId);
            review!.CreatedAt = DateTime.UtcNow.AddDays(-8);
            await db.SaveChangesAsync();
            return 0;
        });

        var row = await MyBookingRowAsync(visit.CustomerId, visit.BookingId);

        row["reviewId"]!.Value<string>().Should().Be(reviewId.ToString());
        row["reviewEditable"]!.Value<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task A_booking_without_a_review_says_so()
    {
        var visit = await CompletedVisitAsync();

        var row = await MyBookingRowAsync(visit.CustomerId, visit.BookingId);

        row["canReview"]!.Value<bool>().Should().BeTrue();
        IsNull(row["reviewBookingId"]).Should().BeTrue();
        row["reviewEditable"]!.Value<bool>().Should().BeFalse();
    }

    private async Task<JToken> MyReviewAsync(Guid customerId, Guid reviewId)
    {
        AsCustomer(customerId);
        var response = await Client.GetAsync("/api/v1/reviews/me");
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        ClearAuthenticationHeader();
        return ((JArray)Data(text)["items"]!).Single(i => i["reviewId"]!.Value<string>() == reviewId.ToString());
    }
}
