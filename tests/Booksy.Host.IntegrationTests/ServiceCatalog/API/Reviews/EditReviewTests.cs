using System.Net;
using System.Net.Http.Json;
using Booksy.ServiceCatalog.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Reviews;

/// <summary>
/// The author's bounded edit (provider-reviews "The author may edit a review within a bounded window";
/// review-moderation "An edited review returns to the queue"). Also the last scenario of task 4.3: an edited
/// published review leaves the provider's rating until it is approved again.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class EditReviewTests : ReviewTestBase
{
    public EditReviewTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private static object AnEdit(decimal rating = 2.0m) => new
    {
        rating,
        comment = "دیر شروع کردند و عجله داشتند",
        punctualityRating = 1.0m,
    };

    private Task<HttpResponseMessage> EditAsync(Guid reviewId, Guid asUser, object? body = null)
    {
        AsCustomer(asUser);
        return Client.PutAsJsonAsync($"/api/v1/reviews/{reviewId}", body ?? AnEdit());
    }

    private async Task ModerateAsync(Guid reviewId, string action, object? body = null)
    {
        AsAdmin();
        var response = await Client.PostAsJsonAsync($"/api/v1/admin/reviews/{reviewId}/{action}", body ?? new { });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        ClearAuthenticationHeader();
    }

    private Task BackdateAsync(Guid reviewId, int days) => FreshAsync(async db =>
    {
        var review = await db.Reviews.SingleAsync(r => r.Id == reviewId);
        review.CreatedAt = DateTime.UtcNow.AddDays(-days);
        await db.SaveChangesAsync();
        return 0;
    });

    [Fact]
    public async Task The_author_can_edit_inside_the_window()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 4.5m);

        var response = await EditAsync(reviewId, visit.CustomerId);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var review = await LoadReviewAsync(reviewId);
        review.RatingValue.Should().Be(2.0m);
        review.PunctualityRating.Should().Be(1.0m);
        review.Comment.Should().Be("دیر شروع کردند و عجله داشتند");
        review.EditedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Editing_a_published_review_takes_it_out_of_the_rating_until_it_is_approved_again()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 5.0m);
        await ModerateAsync(reviewId, "approve");
        (await LoadProviderAsync(visit.Provider.Id)).PublishedReviewCount.Should().Be(1);

        (await EditAsync(reviewId, visit.CustomerId)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await LoadReviewAsync(reviewId)).ModerationStatus.Should().Be(ReviewModerationStatus.Pending);
        var afterEdit = await LoadProviderAsync(visit.Provider.Id);
        afterEdit.PublishedReviewCount.Should().Be(0);

        await ModerateAsync(reviewId, "approve");

        var afterApproval = await LoadProviderAsync(visit.Provider.Id);
        afterApproval.PublishedReviewCount.Should().Be(1);
        afterApproval.AverageRating.Should().Be(2.0m, "the approved text is the edited one");
    }

    [Fact]
    public async Task Editing_after_seven_days_is_refused_and_changes_nothing()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 4.5m);
        await BackdateAsync(reviewId, 8);

        var response = await EditAsync(reviewId, visit.CustomerId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LoadReviewAsync(reviewId)).RatingValue.Should().Be(4.5m);
    }

    [Fact]
    public async Task Someone_else_cannot_edit_the_review()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 4.5m);

        var response = await EditAsync(reviewId, Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await LoadReviewAsync(reviewId)).RatingValue.Should().Be(4.5m);
    }

    [Fact]
    public async Task A_rejected_review_cannot_be_edited_back_into_the_queue()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit);
        await ModerateAsync(reviewId, "reject", new { reason = "contains a phone number" });

        var response = await EditAsync(reviewId, visit.CustomerId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LoadReviewAsync(reviewId)).ModerationStatus.Should().Be(ReviewModerationStatus.Rejected);
    }

    [Fact]
    public async Task A_hidden_review_cannot_be_edited_to_undo_the_hide()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit);
        await ModerateAsync(reviewId, "approve");
        await ModerateAsync(reviewId, "hide", new { reason = "reported and upheld" });

        var response = await EditAsync(reviewId, visit.CustomerId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LoadReviewAsync(reviewId)).ModerationStatus.Should().Be(ReviewModerationStatus.Hidden);
    }

    [Fact]
    public async Task An_invalid_edit_is_refused_naming_the_field()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit);

        var response = await EditAsync(reviewId, visit.CustomerId, new { rating = 4.0m, conductRating = 4.2m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("ConductRating");
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_edit()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit);
        ClearAuthenticationHeader();

        var response = await Client.PutAsJsonAsync($"/api/v1/reviews/{reviewId}", AnEdit());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
