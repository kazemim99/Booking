using System.Security.Claims;
using Booksy.ServiceCatalog.Api.Controllers.V1;
using Booksy.ServiceCatalog.Api.Models.Requests;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.Application.Commands.Review.CastReviewVote;
using Booksy.ServiceCatalog.Application.Commands.Review.EditReview;
using Booksy.ServiceCatalog.Application.Commands.Review.ManageReply;
using Booksy.ServiceCatalog.Application.Commands.Review.ModerateReview;
using Booksy.ServiceCatalog.Application.Commands.Review.ReportReview;
using Booksy.ServiceCatalog.Application.Queries.Review.GetProviderReviews;
using Booksy.ServiceCatalog.Application.Queries.Review.ManagedReviews;
using Booksy.ServiceCatalog.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Booksy.ServiceCatalog.Api.UnitTests.Controllers;

/// <summary>
/// Who the review endpoints think the caller is (task 6.1).
/// </summary>
/// <remarks>
/// Production tokens carry the user id as <see cref="ClaimTypes.NameIdentifier"/>, never <c>sub</c>/<c>userId</c>.
/// A test token minting claims the real token never issues once hid 403s from every real user (the JWT claim-shape
/// defect class). Each principal here carries BOTH a nameidentifier and a different <c>sub</c>, so a lookup that
/// preferred <c>sub</c> would send the wrong id and fail these tests. Behaviour end to end is covered by the
/// integration tests; this pins the identity plumbing only.
/// </remarks>
public class ReviewsControllerIdentityTests
{
    private static readonly Guid RealUser = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid DecoySub = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid ReviewId = Guid.Parse("cccccccc-0000-0000-0000-000000000003");

    private static ClaimsPrincipal RealTokenShape() => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, RealUser.ToString()),
        new Claim("sub", DecoySub.ToString()),
    }, "Test"));

    private static ClaimsPrincipal NoUserId() => new(new ClaimsIdentity(Array.Empty<Claim>(), "Test"));

    private static (T Controller, ISender Sender) Build<T>(Func<ISender, T> make, ClaimsPrincipal user)
        where T : ControllerBase
    {
        var sender = Substitute.For<ISender>();
        var controller = make(sender);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
        return (controller, sender);
    }

    private static (ReviewsController, ISender) Reviews(ClaimsPrincipal user) =>
        Build(s => new ReviewsController(s, NullLogger<ReviewsController>.Instance), user);

    private static GetProviderReviewsViewModel EmptyListing() => new(
        Guid.NewGuid(),
        new ReviewStatisticsViewModel(0, 0, 0m,
            new RatingDistributionViewModel(0, 0, 0, 0, 0, 0m, 0m, 0m, 0m, 0m),
            0, 0, null, null,
            Domain.Policies.DimensionRating.None, Domain.Policies.DimensionRating.None,
            Domain.Policies.DimensionRating.None, Domain.Policies.DimensionRating.None),
        new PaginatedReviewsViewModel(new List<ReviewItemViewModel>(), 0, 1, 20, 0, false, false));

    [Fact]
    public async Task Editing_uses_the_nameidentifier_as_the_editor()
    {
        var (controller, sender) = Reviews(RealTokenShape());
        sender.Send(Arg.Any<EditReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(new EditReviewResult(ReviewId, ReviewModerationStatus.Pending, DateTime.UtcNow));

        await controller.EditReview(ReviewId, new CreateReviewRequest { Rating = 4.0m });

        await sender.Received(1).Send(Arg.Is<EditReviewCommand>(c => c.EditorId == RealUser), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Voting_uses_the_nameidentifier_as_the_voter()
    {
        var (controller, sender) = Reviews(RealTokenShape());
        sender.Send(Arg.Any<CastReviewVoteCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CastReviewVoteResult(ReviewId, 1, 0, 1m, false, true));

        await controller.MarkReviewHelpful(ReviewId, new MarkReviewHelpfulRequest { IsHelpful = true });

        await sender.Received(1).Send(Arg.Is<CastReviewVoteCommand>(c => c.VoterId == RealUser), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reporting_uses_the_nameidentifier_as_the_reporter()
    {
        var (controller, sender) = Reviews(RealTokenShape());
        sender.Send(Arg.Any<ReportReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ReportReviewResult(Guid.NewGuid(), ReviewId));

        await controller.ReportReview(ReviewId, new ReportReviewRequest { Reason = "spam" });

        await sender.Received(1).Send(Arg.Is<ReportReviewCommand>(c => c.ReporterId == RealUser), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replying_records_the_nameidentifier_as_the_actor()
    {
        var (controller, sender) = Reviews(RealTokenShape());
        sender.Send(Arg.Any<ManageReplyCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ManageReplyResult(ReviewId, "ممنون", ReviewModerationStatus.Pending));

        await controller.AddReply(ReviewId, new ReplyRequest { Text = "ممنون" }, CancellationToken.None);

        await sender.Received(1).Send(
            Arg.Is<ManageReplyCommand>(c => c.ActedBy == $"Provider:{RealUser}"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_own_review_list_is_the_nameidentifiers()
    {
        var (controller, sender) = Reviews(RealTokenShape());
        sender.Send(Arg.Any<GetMyReviewsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ManagedReviewsViewModel(Array.Empty<ManagedReviewItem>(), 0));

        await controller.GetMyReviews();

        await sender.Received(1).Send(Arg.Is<GetMyReviewsQuery>(q => q.CustomerId == RealUser), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_signed_in_reader_of_the_public_listing_is_identified_by_nameidentifier()
    {
        var (controller, sender) = Reviews(RealTokenShape());
        sender.Send(Arg.Any<GetProviderReviewsQuery>(), Arg.Any<CancellationToken>()).Returns(EmptyListing());

        await controller.GetProviderReviews(Guid.NewGuid(), new GetProviderReviewsRequest());

        await sender.Received(1).Send(
            Arg.Is<GetProviderReviewsQuery>(q => q.CallerId == RealUser), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Moderation_records_the_nameidentifier_as_the_moderator()
    {
        var (controller, sender) = Build(s => new ReviewModerationController(s), RealTokenShape());
        sender.Send(Arg.Any<ModerateReviewCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ModerateReviewResult(ReviewId, ReviewModerationStatus.Published, null));

        await controller.Approve(ReviewId, CancellationToken.None);

        await sender.Received(1).Send(
            Arg.Is<ModerateReviewCommand>(c => c.ModeratedBy == $"Admin:{RealUser}"), Arg.Any<CancellationToken>());
    }

    // ── No identity at all ──

    [Fact]
    public async Task A_vote_without_a_user_id_is_refused_and_nothing_is_sent()
    {
        var (controller, sender) = Reviews(NoUserId());

        var result = await controller.MarkReviewHelpful(ReviewId, new MarkReviewHelpfulRequest { IsHelpful = true });

        result.Should().BeOfType<UnauthorizedObjectResult>();
        await sender.DidNotReceive().Send(Arg.Any<CastReviewVoteCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_edit_without_a_user_id_is_refused_and_nothing_is_sent()
    {
        var (controller, sender) = Reviews(NoUserId());

        var result = await controller.EditReview(ReviewId, new CreateReviewRequest { Rating = 4.0m });

        result.Should().BeOfType<UnauthorizedObjectResult>();
        await sender.DidNotReceive().Send(Arg.Any<EditReviewCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_public_listing_still_works_with_nobody_signed_in()
    {
        var (controller, sender) = Reviews(new ClaimsPrincipal(new ClaimsIdentity()));
        sender.Send(Arg.Any<GetProviderReviewsQuery>(), Arg.Any<CancellationToken>()).Returns(EmptyListing());

        await controller.GetProviderReviews(Guid.NewGuid(), new GetProviderReviewsRequest());

        await sender.Received(1).Send(
            Arg.Is<GetProviderReviewsQuery>(q => q.CallerId == null), Arg.Any<CancellationToken>());
    }
}
