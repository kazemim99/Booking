using System.Reflection;
using Booksy.API.RateLimiting;
using Booksy.ServiceCatalog.Api.Controllers.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;

namespace Booksy.ServiceCatalog.Api.UnitTests.Controllers;

/// <summary>
/// Rate limits on the review write paths (task 6.6, design D12).
/// </summary>
/// <remarks>
/// <para><b>Why the registration check matters.</b> <c>RateLimitingRegistration</c> registers exactly the names in
/// <see cref="RateLimitingOptions.Defaults"/>, and an <c>[EnableRateLimiting]</c> naming anything else throws at
/// request time — including in the test host, where limiting is disabled but each name is still registered as a
/// no-op. The failure is a 500 on the first call, not a missing limit, and nothing else would catch it before
/// production.</para>
///
/// <para><b>Why edit is the tightest.</b> An edit unpublishes a review and forces a full per-provider rating
/// recompute, and the trigger is an ordinary customer, repeatable for seven days.</para>
/// </remarks>
public class ReviewRateLimitingTests
{
    private static readonly Assembly ApiAssembly = typeof(ReviewsController).Assembly;

    private static IEnumerable<(MethodInfo Action, string Policy)> RateLimitedActions() =>
        ApiAssembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Select(m => (Action: m, Attribute: m.GetCustomAttribute<EnableRateLimitingAttribute>()
                                                 ?? m.DeclaringType!.GetCustomAttribute<EnableRateLimitingAttribute>()))
            .Where(x => x.Attribute is not null)
            .Select(x => (x.Action, x.Attribute!.PolicyName!));

    [Fact]
    public void Every_rate_limit_policy_named_on_an_endpoint_is_registered()
    {
        var unregistered = RateLimitedActions()
            .Where(x => !RateLimitingOptions.Defaults.ContainsKey(x.Policy))
            .Select(x => $"{x.Action.DeclaringType!.Name}.{x.Action.Name} → '{x.Policy}'")
            .ToList();

        Assert.True(unregistered.Count == 0,
            "These endpoints name a policy RateLimitingOptions.Defaults does not register, so they 500 on first call:\n"
            + string.Join("\n", unregistered));
    }

    public static TheoryData<Type, string> ReviewWritePaths => new()
    {
        { typeof(ReviewsController), nameof(ReviewsController.CreateReview) },
        { typeof(ReviewsController), nameof(ReviewsController.EditReview) },
        { typeof(ReviewsController), nameof(ReviewsController.MarkReviewHelpful) },
        { typeof(ReviewsController), nameof(ReviewsController.ReportReview) },
        { typeof(ReviewsController), nameof(ReviewsController.AddReply) },
        { typeof(ReviewsController), nameof(ReviewsController.EditReply) },
        { typeof(ReviewsController), nameof(ReviewsController.RemoveReply) },
        { typeof(ReviewModerationController), nameof(ReviewModerationController.Approve) },
        { typeof(ReviewModerationController), nameof(ReviewModerationController.Reject) },
        { typeof(ReviewModerationController), nameof(ReviewModerationController.Hide) },
        { typeof(ReviewModerationController), nameof(ReviewModerationController.Restore) },
        { typeof(ReviewModerationController), nameof(ReviewModerationController.ApproveReply) },
        { typeof(ReviewModerationController), nameof(ReviewModerationController.RejectReply) },
    };

    [Theory]
    [MemberData(nameof(ReviewWritePaths))]
    public void Every_review_write_path_is_rate_limited(Type controller, string action)
    {
        var method = controller.GetMethod(action)!;
        var attribute = method.GetCustomAttribute<EnableRateLimitingAttribute>()
                        ?? controller.GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.True(attribute is not null, $"{controller.Name}.{action} has no [EnableRateLimiting]");
        Assert.True(method.GetCustomAttributes<HttpMethodAttribute>().Any(), $"{controller.Name}.{action} is not an endpoint");
    }

    [Fact]
    public void Editing_a_review_has_the_tightest_ceiling_of_the_customer_write_paths()
    {
        static RateLimitPolicyOptions PolicyOf(string action) =>
            RateLimitingOptions.Defaults[typeof(ReviewsController).GetMethod(action)!
                .GetCustomAttribute<EnableRateLimitingAttribute>()!.PolicyName!];

        static double PerHour(RateLimitPolicyOptions o) => o.PermitLimit * 3600.0 / o.WindowSeconds;

        var edit = PerHour(PolicyOf(nameof(ReviewsController.EditReview)));
        foreach (var other in new[]
                 {
                     nameof(ReviewsController.CreateReview), nameof(ReviewsController.MarkReviewHelpful),
                     nameof(ReviewsController.ReportReview), nameof(ReviewsController.AddReply),
                 })
        {
            Assert.True(edit <= PerHour(PolicyOf(other)), $"edit allows more per hour than {other}");
        }
    }
}
