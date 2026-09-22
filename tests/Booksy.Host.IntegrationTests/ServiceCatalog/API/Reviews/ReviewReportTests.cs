using System.Net;
using System.Net.Http.Json;
using Booksy.ServiceCatalog.Domain.Enums;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Reviews;

/// <summary>
/// Reporting a published review (review-moderation "Anyone may report a published review"). A report never takes
/// anything down on its own — the review stays public until an administrator acts on it.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class ReviewReportTests : ReviewTestBase
{
    public ReviewReportTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private async Task<(Guid ReviewId, Visit Visit)> PublishedReviewAsync()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 1.0m);
        AsAdmin();
        (await Client.PostAsJsonAsync($"/api/v1/admin/reviews/{reviewId}/approve", new { }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        ClearAuthenticationHeader();
        return (reviewId, visit);
    }

    private Task<HttpResponseMessage> ReportAsync(Guid reviewId, string reason = "توهین به کارکنان") =>
        Client.PostAsJsonAsync($"/api/v1/reviews/{reviewId}/report", new { reason });

    private async Task<JArray> ReportedQueueAsync()
    {
        AsAdmin();
        var response = await Client.GetAsync("/api/v1/admin/reviews/queue?filter=reported&pageSize=50");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return (JArray)Data(body)["items"]!;
    }

    [Fact]
    public async Task A_reported_review_stays_public_and_reaches_the_administrator_with_who_and_why()
    {
        var (reviewId, _) = await PublishedReviewAsync();
        var reporter = Guid.NewGuid();
        AsCustomer(reporter);

        var response = await ReportAsync(reviewId, "حاوی شماره تلفن است");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await LoadReviewAsync(reviewId)).ModerationStatus.Should().Be(ReviewModerationStatus.Published,
            "a report never takes anything down by itself");

        var item = (await ReportedQueueAsync()).Single(i => i["reviewId"]!.Value<string>() == reviewId.ToString());
        item["reportCount"]!.Value<int>().Should().Be(1);
        var report = item["reports"]![0]!;
        report["reason"]!.Value<string>().Should().Be("حاوی شماره تلفن است");
        report["reportedByUserId"]!.Value<string>().Should().Be(reporter.ToString());
    }

    [Fact]
    public async Task The_most_reported_reviews_come_first()
    {
        var (once, _) = await PublishedReviewAsync();
        var (twice, _) = await PublishedReviewAsync();
        AsCustomer(Guid.NewGuid()); await ReportAsync(once);
        AsCustomer(Guid.NewGuid()); await ReportAsync(twice);
        AsCustomer(Guid.NewGuid()); await ReportAsync(twice);

        var ids = (await ReportedQueueAsync()).Select(i => Guid.Parse(i["reviewId"]!.Value<string>()!)).ToList();

        ids.Should().Equal(twice, once);
    }

    [Fact]
    public async Task The_same_user_cannot_report_the_same_review_twice()
    {
        var (reviewId, _) = await PublishedReviewAsync();
        AsCustomer(Guid.NewGuid());
        await ReportAsync(reviewId);

        var response = await ReportAsync(reviewId, "دوباره");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReportedQueueAsync()).Single()["reportCount"]!.Value<int>().Should().Be(1);
    }

    [Fact]
    public async Task The_owning_provider_can_report_a_review_of_their_business()
    {
        var (reviewId, visit) = await PublishedReviewAsync();
        AuthenticateAsProviderOwner(visit.Provider);

        var response = await ReportAsync(reviewId, "این مشتری هیچ‌وقت نیامد");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_report_needs_a_reason()
    {
        var (reviewId, _) = await PublishedReviewAsync();
        AsCustomer(Guid.NewGuid());

        var response = await ReportAsync(reviewId, "  ");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_review_that_is_not_public_cannot_be_reported()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit);
        AsCustomer(Guid.NewGuid());

        var response = await ReportAsync(reviewId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_report()
    {
        var (reviewId, _) = await PublishedReviewAsync();
        ClearAuthenticationHeader();

        var response = await ReportAsync(reviewId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Hiding_a_reported_review_takes_it_off_the_reported_list()
    {
        var (reviewId, _) = await PublishedReviewAsync();
        AsCustomer(Guid.NewGuid());
        await ReportAsync(reviewId);

        AsAdmin();
        (await Client.PostAsJsonAsync($"/api/v1/admin/reviews/{reviewId}/hide", new { reason = "reported and upheld" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await ReportedQueueAsync()).Should().BeEmpty();
    }
}
