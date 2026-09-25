using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.ReviewAggregate;

/// <summary>
/// The two records other users attach to a review: one vote each, and reports. Both live in their own tables,
/// never as collections loaded into the review — see design D4.
/// </summary>
public class ReviewVoteAndReportTests
{
    private static readonly DateTime Now = new(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid ReviewId = Guid.NewGuid();
    private static readonly UserId Voter = UserId.From(Guid.NewGuid());

    // ── Vote ──

    [Fact]
    public void A_vote_records_who_on_which_review_and_which_way()
    {
        var vote = ReviewVote.Cast(ReviewId, Voter, isHelpful: true, Now);

        Assert.Equal(ReviewId, vote.ReviewId);
        Assert.Equal(Voter, vote.UserId);
        Assert.True(vote.IsHelpful);
        Assert.Equal(Now, vote.CreatedAt);
        Assert.NotEqual(Guid.Empty, vote.Id);
    }

    [Fact]
    public void A_vote_can_change_direction()
    {
        var vote = ReviewVote.Cast(ReviewId, Voter, isHelpful: true, Now);

        vote.ChangeTo(isHelpful: false, Now.AddMinutes(5));

        Assert.False(vote.IsHelpful);
        Assert.Equal(Now.AddMinutes(5), vote.LastModifiedAt);
    }

    // ── Report ──

    [Fact]
    public void A_report_records_the_trimmed_reason_and_the_reporter()
    {
        var report = ReviewReport.File(ReviewId, Voter, "  توهین به کارکنان  ", Now);

        Assert.Equal(ReviewId, report.ReviewId);
        Assert.Equal(Voter, report.ReportedByUserId);
        Assert.Equal("توهین به کارکنان", report.Reason);
        Assert.Equal(Now, report.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_report_without_a_reason_is_refused(string reason)
    {
        var ex = Assert.Throws<DomainValidationException>(() => ReviewReport.File(ReviewId, Voter, reason, Now));

        Assert.True(ex.ValidationErrors.ContainsKey(nameof(ReviewReport.Reason)));
    }

    [Fact]
    public void A_report_reason_over_500_characters_is_refused()
    {
        Assert.Throws<DomainValidationException>(() => ReviewReport.File(ReviewId, Voter, new string('x', 501), Now));
    }
}
