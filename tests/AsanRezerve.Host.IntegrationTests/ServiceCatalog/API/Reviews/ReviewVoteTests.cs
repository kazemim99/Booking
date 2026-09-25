using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Reviews;

/// <summary>
/// Helpful / not-helpful votes (review-engagement). One authenticated vote per user per review, changeable and
/// withdrawable; counts that always equal the distinct voters plus the frozen legacy baseline; and everything
/// derived from them derived from that same sum.
/// </summary>
/// <remarks>
/// Before this change the endpoint was <c>[AllowAnonymous]</c> and a bare <c>count++</c>: anyone could inflate a
/// review without limit. <b>BREAKING</b> for any client that called it without a token (task 6.3).
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class ReviewVoteTests : ReviewTestBase
{
    public ReviewVoteTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    private async Task<(Guid ReviewId, Visit Visit)> PublishedReviewAsync()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit, 4.0m);
        AsAdmin();
        (await Client.PostAsJsonAsync($"/api/v1/admin/reviews/{reviewId}/approve", new { }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        ClearAuthenticationHeader();
        return (reviewId, visit);
    }

    private async Task<JToken> VoteAsync(Guid reviewId, Guid voter, bool helpful)
    {
        AsCustomer(voter);
        var response = await Client.PutAsJsonAsync($"/api/v1/reviews/{reviewId}/helpful", new { isHelpful = helpful });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return Data(body);
    }

    private Task<int> VoteRowsAsync(Guid reviewId) =>
        FreshAsync(db => db.ReviewVotes.CountAsync(v => v.ReviewId == reviewId));

    private Task SetLegacyBaselineAsync(Guid reviewId, int helpful, int notHelpful) =>
        FreshAsync(db => db.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "ServiceCatalog"."Reviews" SET "HelpfulCount" = {helpful}, "NotHelpfulCount" = {notHelpful} WHERE "ReviewId" = {reviewId}"""));

    [Fact]
    public async Task A_first_vote_counts_once_and_the_voter_is_told_their_vote()
    {
        var (reviewId, _) = await PublishedReviewAsync();

        var result = await VoteAsync(reviewId, Guid.NewGuid(), helpful: true);

        result["helpfulCount"]!.Value<int>().Should().Be(1);
        result["notHelpfulCount"]!.Value<int>().Should().Be(0);
        result["myVote"]!.Value<string>().Should().Be("helpful");
    }

    [Fact]
    public async Task Repeating_the_same_vote_withdraws_it()
    {
        var (reviewId, _) = await PublishedReviewAsync();
        var voter = Guid.NewGuid();
        await VoteAsync(reviewId, voter, helpful: true);

        var result = await VoteAsync(reviewId, voter, helpful: true);

        result["helpfulCount"]!.Value<int>().Should().Be(0);
        // The host omits null properties, so "no vote" arrives as an absent myVote.
        (result["myVote"] is null || result["myVote"]!.Type == JTokenType.Null).Should().BeTrue();
        (await VoteRowsAsync(reviewId)).Should().Be(0);
    }

    [Fact]
    public async Task Changing_a_vote_moves_it_and_never_double_counts()
    {
        var (reviewId, _) = await PublishedReviewAsync();
        var voter = Guid.NewGuid();
        await VoteAsync(reviewId, voter, helpful: true);

        var result = await VoteAsync(reviewId, voter, helpful: false);

        result["helpfulCount"]!.Value<int>().Should().Be(0);
        result["notHelpfulCount"]!.Value<int>().Should().Be(1);
        result["myVote"]!.Value<string>().Should().Be("notHelpful");
        (await VoteRowsAsync(reviewId)).Should().Be(1);
    }

    [Fact]
    public async Task Two_people_voting_count_twice()
    {
        var (reviewId, _) = await PublishedReviewAsync();

        await VoteAsync(reviewId, Guid.NewGuid(), helpful: true);
        var result = await VoteAsync(reviewId, Guid.NewGuid(), helpful: true);

        result["helpfulCount"]!.Value<int>().Should().Be(2);
    }

    [Fact]
    public async Task Fifty_simultaneous_votes_from_one_user_leave_at_most_one_vote_and_a_count_that_matches()
    {
        var (reviewId, _) = await PublishedReviewAsync();
        AsCustomer(Guid.NewGuid());

        var responses = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ =>
            Client.PutAsJsonAsync($"/api/v1/reviews/{reviewId}/helpful", new { isHelpful = true })));

        responses.Should().NotContain(r => (int)r.StatusCode >= 500, "a lost race is a conflict, not a server error");
        var rows = await VoteRowsAsync(reviewId);
        rows.Should().BeLessThanOrEqualTo(1);
        (await LoadReviewAsync(reviewId)).HelpfulVoteCount.Should().Be(rows, "the tally is exactly the vote rows");
    }

    [Fact]
    public async Task The_author_cannot_vote_on_their_own_review()
    {
        var (reviewId, visit) = await PublishedReviewAsync();
        AsCustomer(visit.CustomerId);

        var response = await Client.PutAsJsonAsync($"/api/v1/reviews/{reviewId}/helpful", new { isHelpful = true });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await VoteRowsAsync(reviewId)).Should().Be(0);
        JObject.Parse(await response.Content.ReadAsStringAsync())["message"]!.Value<string>()
            .Should().Be("به نظر خودتان نمی‌توانید رأی بدهید.", "the app shows the server's reason, which was English");
    }

    [Fact]
    public async Task A_review_awaiting_moderation_cannot_be_voted_on()
    {
        var visit = await CompletedVisitAsync();
        var reviewId = await ReviewedAsync(visit);
        AsCustomer(Guid.NewGuid());

        var response = await Client.PutAsJsonAsync($"/api/v1/reviews/{reviewId}/helpful", new { isHelpful = true });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await VoteRowsAsync(reviewId)).Should().Be(0);
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_vote_any_more()
    {
        var (reviewId, _) = await PublishedReviewAsync();
        ClearAuthenticationHeader();

        var response = await Client.PutAsJsonAsync($"/api/v1/reviews/{reviewId}/helpful", new { isHelpful = true });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await LoadReviewAsync(reviewId)).HelpfulCount.Should().Be(0);
    }

    [Fact]
    public async Task A_review_written_after_the_freeze_can_become_helpful_on_live_votes_alone()
    {
        // Its legacy baseline is 0/0 forever. If "helpful" were judged on the baseline alone, it never could be.
        var (reviewId, _) = await PublishedReviewAsync();

        JToken result = null!;
        for (var i = 0; i < 5; i++)
            result = await VoteAsync(reviewId, Guid.NewGuid(), helpful: true);

        (await LoadReviewAsync(reviewId)).LegacyHelpfulCount.Should().Be(0);
        result["helpfulCount"]!.Value<int>().Should().Be(5);
        result["helpfulnessRatio"]!.Value<decimal>().Should().Be(1m);
        result["isConsideredHelpful"]!.Value<bool>().Should().BeTrue();
    }

    // ── The legacy baseline ──

    [Fact]
    public async Task A_new_vote_adds_to_the_legacy_baseline_and_the_ratio_is_over_both()
    {
        var (reviewId, _) = await PublishedReviewAsync();
        await SetLegacyBaselineAsync(reviewId, helpful: 7, notHelpful: 2);

        var result = await VoteAsync(reviewId, Guid.NewGuid(), helpful: false);

        result["helpfulCount"]!.Value<int>().Should().Be(7);
        result["notHelpfulCount"]!.Value<int>().Should().Be(3);
        result["helpfulnessRatio"]!.Value<decimal>().Should().Be(0.7m);
        result["isConsideredHelpful"]!.Value<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task Withdrawing_a_vote_never_takes_the_count_below_the_baseline()
    {
        var (reviewId, _) = await PublishedReviewAsync();
        await SetLegacyBaselineAsync(reviewId, helpful: 7, notHelpful: 0);
        var voter = Guid.NewGuid();
        await VoteAsync(reviewId, voter, helpful: true);

        var result = await VoteAsync(reviewId, voter, helpful: true);

        result["helpfulCount"]!.Value<int>().Should().Be(7);
        var review = await LoadReviewAsync(reviewId);
        review.LegacyHelpfulCount.Should().Be(7, "the baseline is never written");
        review.HelpfulVoteCount.Should().Be(0);
    }

    [Fact]
    public async Task Sorting_by_helpfulness_puts_a_review_with_more_live_votes_above_a_baseline()
    {
        // Spec: a review with no baseline that collects more live votes than one with a baseline is ordered above
        // it. Ordering on the frozen baseline alone would pin the sort at deploy day.
        var first = await CompletedVisitAsync();
        var withBaseline = await ReviewedAsync(first, 4.0m);
        var liveOnly = await ReviewedAsync(await CompletedVisitAsync(first.Provider), 4.0m);
        AsAdmin();
        foreach (var id in new[] { withBaseline, liveOnly })
            (await Client.PostAsJsonAsync($"/api/v1/admin/reviews/{id}/approve", new { }))
                .StatusCode.Should().Be(HttpStatusCode.OK);
        ClearAuthenticationHeader();
        await SetLegacyBaselineAsync(withBaseline, helpful: 2, notHelpful: 0);
        for (var i = 0; i < 3; i++)
            await VoteAsync(liveOnly, Guid.NewGuid(), helpful: true);
        ClearAuthenticationHeader();

        var response = await Client.GetAsync(
            $"/api/v1/reviews/providers/{first.Provider.Id.Value}?sortBy=helpful&sortDescending=true");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var order = ((JArray)Data(body)["reviews"]!["items"]!)
            .Select(i => Guid.Parse(i["reviewId"]!.Value<string>()!))
            .ToList();
        order.Should().Equal(new[] { liveOnly, withBaseline }, "3 live votes outrank a baseline of 2");
    }
}
