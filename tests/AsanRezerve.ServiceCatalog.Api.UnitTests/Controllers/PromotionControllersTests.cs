using System.Reflection;
using System.Security.Claims;
using AsanRezerve.ServiceCatalog.Api.Controllers.V1;
using AsanRezerve.ServiceCatalog.API.Controllers.V1;
using AsanRezerve.ServiceCatalog.API.Models.Requests;
using AsanRezerve.ServiceCatalog.Application.Commands.Booking.CreateBooking;
using AsanRezerve.ServiceCatalog.Application.Promotions;
using AsanRezerve.ServiceCatalog.Application.Queries.Membership.CanManageOrganization;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NSubstitute;

namespace AsanRezerve.ServiceCatalog.Api.UnitTests.Controllers;

/// <summary>
/// Who may reach the discount endpoints and what identity they forward (openspec/changes/add-discounts-and-campaigns,
/// design D11). Behaviour end to end is the integration suite's; this pins authorization and request mapping.
/// </summary>
public class PromotionControllersTests
{
    private static readonly Guid Salon = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid User = Guid.Parse("22222222-0000-0000-0000-000000000002");

    private static ClaimsPrincipal Principal(params Claim[] claims) => new(new ClaimsIdentity(claims, "Test"));

    private static ClaimsPrincipal Owner() => Principal(
        new Claim(ClaimTypes.NameIdentifier, User.ToString()), new Claim("providerId", Salon.ToString()));

    private static ClaimsPrincipal Member() => Principal(new Claim(ClaimTypes.NameIdentifier, User.ToString()));

    private static ClaimsPrincipal Admin() => Principal(
        new Claim(ClaimTypes.NameIdentifier, User.ToString()), new Claim(ClaimTypes.Role, "Admin"));

    private static (ProviderPromotionsController Controller, ISender Sender) Salons(ClaimsPrincipal user)
    {
        var sender = Substitute.For<ISender>();
        var controller = new ProviderPromotionsController(sender)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } }
        };
        return (controller, sender);
    }

    private static PromotionTermsInput Terms() =>
        new("تخفیف", null, "Automatic", null, "Percentage", 20, null, null, false, null, null, null, null, null, null, null, null);

    [Fact]
    public async Task An_anonymous_caller_cannot_list_a_salons_promotions()
    {
        var (controller, sender) = Salons(Principal());

        var result = await controller.List(Salon, default);

        result.Should().BeOfType<ForbidResult>();
        await sender.DidNotReceive().Send(Arg.Any<GetProviderPromotionsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_owner_of_the_salon_may_create_and_is_recorded_as_the_creator()
    {
        var (controller, sender) = Salons(Owner());

        var result = await controller.Create(Salon, Terms(), default);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        await sender.Received(1).Send(
            Arg.Is<CreateProviderPromotionCommand>(c => c.ProviderId == Salon && c.ActingUserId == User),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_member_needs_the_manage_organization_permission_not_just_bookings()
    {
        var (controller, sender) = Salons(Member());
        sender.Send(Arg.Any<CanManageOrganizationQuery>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await controller.Create(Salon, Terms(), default);

        result.Should().BeOfType<ForbidResult>();
        await sender.Received(1).Send(
            Arg.Is<CanManageOrganizationQuery>(q => q.Permission == OrganizationPermission.ManageOrganization),
            Arg.Any<CancellationToken>());
        await sender.DidNotReceive().Send(Arg.Any<CreateProviderPromotionCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_managing_member_may_pause()
    {
        var (controller, sender) = Salons(Member());
        sender.Send(Arg.Any<CanManageOrganizationQuery>(), Arg.Any<CancellationToken>()).Returns(true);
        var promotionId = Guid.NewGuid();

        var result = await controller.Pause(Salon, promotionId, default);

        result.Should().BeOfType<OkObjectResult>();
        await sender.Received(1).Send(
            Arg.Is<ChangeProviderPromotionStatusCommand>(c =>
                c.ProviderId == Salon && c.PromotionId == promotionId && c.Action == PromotionLifecycleAction.Pause),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_owner_of_another_salon_is_refused()
    {
        var (controller, sender) = Salons(Principal(
            new Claim(ClaimTypes.NameIdentifier, User.ToString()), new Claim("providerId", Guid.NewGuid().ToString())));
        sender.Send(Arg.Any<CanManageOrganizationQuery>(), Arg.Any<CancellationToken>()).Returns(false);

        (await controller.Join(Salon, Guid.NewGuid(), default)).Should().BeOfType<ForbidResult>();
        (await controller.Campaigns(Salon, default)).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task An_admin_may_manage_any_salons_promotions()
    {
        var (controller, _) = Salons(Admin());

        (await controller.List(Salon, default)).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Joining_and_leaving_record_the_acting_user()
    {
        var (controller, sender) = Salons(Owner());
        var campaign = Guid.NewGuid();

        await controller.Join(Salon, campaign, default);
        await controller.Leave(Salon, campaign, default);

        await sender.Received(1).Send(
            Arg.Is<JoinCampaignCommand>(c => c.CampaignId == campaign && c.ActingUserId == User), Arg.Any<CancellationToken>());
        await sender.Received(1).Send(
            Arg.Is<LeaveCampaignCommand>(c => c.CampaignId == campaign && c.ActingUserId == User), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void The_public_offers_list_is_anonymous_and_rate_limited()
    {
        var method = typeof(ProviderPromotionsController).GetMethod(nameof(ProviderPromotionsController.Offers))!;

        method.GetCustomAttribute<AllowAnonymousAttribute>().Should().NotBeNull();
        method.GetCustomAttribute<EnableRateLimitingAttribute>()!.PolicyName.Should().Be("public-api");
    }

    [Fact]
    public void Every_other_salon_route_requires_authentication()
    {
        typeof(ProviderPromotionsController).GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
        typeof(ProviderPromotionsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name != nameof(ProviderPromotionsController.Offers))
            .Should().OnlyContain(m => m.GetCustomAttribute<AllowAnonymousAttribute>() == null);
    }

    [Fact]
    public void The_admin_controller_uses_the_AdminOnly_policy_and_nothing_opts_out()
    {
        typeof(AdminPromotionsController).GetCustomAttribute<AuthorizeAttribute>()!.Policy.Should().Be("AdminOnly");
        typeof(AdminPromotionsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Should().OnlyContain(m => m.GetCustomAttribute<AllowAnonymousAttribute>() == null);
    }

    [Fact]
    public async Task An_admin_campaign_records_its_creator()
    {
        var sender = Substitute.For<ISender>();
        var controller = new AdminPromotionsController(sender)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Admin() } }
        };

        await controller.Create(Terms(), default);

        await sender.Received(1).Send(
            Arg.Is<CreatePlatformCampaignCommand>(c => c.ActingUserId == User), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void The_quote_endpoint_is_authenticated_and_has_its_own_rate_limit()
    {
        var method = typeof(BookingsController).GetMethod(nameof(BookingsController.QuotePrice))!;

        method.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
        method.GetCustomAttribute<EnableRateLimitingAttribute>()!.PolicyName.Should().Be("promotion-quote");
        AsanRezerve.API.RateLimiting.RateLimitingOptions.Defaults.Should().ContainKey("promotion-quote");
    }
}
