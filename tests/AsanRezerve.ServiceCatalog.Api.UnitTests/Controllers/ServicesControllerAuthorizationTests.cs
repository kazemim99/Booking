using System.Security.Claims;
using AsanRezerve.ServiceCatalog.API.Controllers.V1;
using AsanRezerve.ServiceCatalog.Application.Commands.Service.ActivateService;
using AsanRezerve.ServiceCatalog.Application.Commands.Service.ArchiveService;
using AsanRezerve.ServiceCatalog.Application.Commands.Service.DeactivateService;
using AsanRezerve.ServiceCatalog.Application.Commands.Service.DeleteProviderService;
using AsanRezerve.ServiceCatalog.Application.Commands.Service.UpdateProviderService;
using AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetOwnedProviderStatus;
using AsanRezerve.ServiceCatalog.Application.Queries.Service.GetServiceById;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AsanRezerve.ServiceCatalog.Api.UnitTests.Controllers;

/// <summary>
/// Regression cover for the service-lifecycle authorization defect (D1).
///
/// <para><b>The bug.</b> <c>ServicesController</c> read the caller's id from a <c>sub</c>/<c>userId</c> claim, but the
/// platform's token service only ever issues <see cref="ClaimTypes.NameIdentifier"/>. The lookup returned null for
/// every valid token and the authorization helpers fail closed on null, so <c>POST {id}/activate</c>,
/// <c>POST {id}/deactivate</c> and <c>DELETE {id}</c> answered <b>403 to every caller, including admins</b>.</para>
///
/// <para><b>The trap in fixing it.</b> Repairing only the claim lookup would have exposed a second, latent defect:
/// <c>CanManageService</c> never resolved the service's owner — it returned <c>User.IsInRole("Provider")</c>, so any
/// provider could have deactivated or archived another business's service. These tests therefore pin both halves:
/// the owner and admin paths must succeed, and a <i>different</i> provider must still be refused.</para>
///
/// Plain unit test: the controller is exercised directly with a substituted mediator.
/// </summary>
public class ServicesControllerAuthorizationTests
{
    private static readonly Guid ServiceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OwningProviderId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherProviderId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid UserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    /// <summary>
    /// A principal shaped like a real token: the id lives in <see cref="ClaimTypes.NameIdentifier"/> — never in
    /// <c>sub</c> or <c>userId</c>, which is precisely what the defect assumed.
    /// </summary>
    private static ClaimsPrincipal Principal(Guid? providerId = null, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, UserId.ToString()) };

        if (providerId.HasValue)
            claims.Add(new Claim("providerId", providerId.Value.ToString()));

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }

    /// <summary>Anonymous-shaped principal: authenticated but carrying no user id at all.</summary>
    private static ClaimsPrincipal PrincipalWithoutUserId() =>
        new(new ClaimsIdentity(new[] { new Claim("providerId", OwningProviderId.ToString()) }, "Test"));

    private static ServicesController BuildController(
        ClaimsPrincipal user,
        ISender mediator)
    {
        var controller = new ServicesController(mediator, NullLogger<ServicesController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user },
            },
        };

        return controller;
    }

    /// <summary>
    /// Wires the mediator so the service resolves to <paramref name="owningProviderId"/> and every lifecycle command
    /// succeeds. Authorization is therefore the only thing under test.
    /// </summary>
    private static ISender Mediator(
        Guid? owningProviderId = null,
        Guid? currentProviderStatusId = null)
    {
        var mediator = Substitute.For<ISender>();

        mediator
            .Send(Arg.Any<GetServiceByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(owningProviderId is null
                ? null
                : new ServiceDetailsViewModel { Id = ServiceId, ProviderId = owningProviderId.Value });

        // The claim-less fallback path: resolves the caller's provider in-process.
        if (currentProviderStatusId is null)
        {
            mediator
                .Send(Arg.Any<GetOwnedProviderStatusQuery>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("no provider for this user"));
        }
        else
        {
            mediator
            .Send(Arg.Any<GetOwnedProviderStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ProviderStatusResult(
                    ProviderId: currentProviderStatusId.Value,
                    Status: ProviderStatus.Active,
                    UserId: UserId));
        }

        mediator
            .Send(Arg.Any<ActivateServiceCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ActivateServiceResult(ServiceId, "Haircut", OwningProviderId, DateTime.UtcNow));

        mediator
            .Send(Arg.Any<DeactivateServiceCommand>(), Arg.Any<CancellationToken>())
            .Returns(new DeactivateServiceResult(ServiceId, "Haircut", OwningProviderId, "reason"));

        mediator
            .Send(Arg.Any<ArchiveServiceCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ArchiveServiceResult(ServiceId, "Haircut", OwningProviderId, "reason"));

        mediator
            .Send(Arg.Any<UpdateProviderServiceCommand>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateProviderServiceResult(ServiceId, "Haircut", 100m, 30, DateTime.UtcNow));

        mediator
            .Send(Arg.Any<DeleteProviderServiceCommand>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteProviderServiceResult(ServiceId, true, "Service deleted successfully"));

        return mediator;
    }

    private static UpdateProviderServiceRequest UpdateRequest() => new()
    {
        ServiceName = "Haircut",
        DurationHours = 0,
        DurationMinutes = 30,
        Price = 100m,
    };

    // ---------------------------------------------------------------- the owner path (was 403 for everyone)

    [Fact]
    public async Task Owning_provider_can_activate_its_own_service()
    {
        var mediator = Mediator(owningProviderId: OwningProviderId);
        var controller = BuildController(Principal(OwningProviderId, "Provider"), mediator);

        var result = await controller.ActivateService(ServiceId);

        result.Should().BeOfType<OkObjectResult>(
            "the owning provider must be able to activate its own service — this returned 403 before the fix");
    }

    [Fact]
    public async Task Owning_provider_can_deactivate_its_own_service()
    {
        var mediator = Mediator(owningProviderId: OwningProviderId);
        var controller = BuildController(Principal(OwningProviderId, "Provider"), mediator);

        var result = await controller.DeactivateService(ServiceId, request: null);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Owning_provider_can_archive_its_own_service()
    {
        var mediator = Mediator(owningProviderId: OwningProviderId);
        var controller = BuildController(Principal(OwningProviderId, "Provider"), mediator);

        var result = await controller.ArchiveService(ServiceId, request: null);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Owning_provider_is_recognised_without_a_providerId_claim()
    {
        // A token minted before the provider existed carries no providerId claim; ownership is resolved in-process
        // instead. Without this the fix would simply have replaced one spurious 403 with another.
        var mediator = Mediator(owningProviderId: OwningProviderId, currentProviderStatusId: OwningProviderId);
        var controller = BuildController(Principal(providerId: null, "Provider"), mediator);

        var result = await controller.ActivateService(ServiceId);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Owning_provider_can_update_its_own_service()
    {
        var mediator = Mediator(owningProviderId: OwningProviderId);
        var controller = BuildController(Principal(OwningProviderId, "Provider"), mediator);

        var result = await controller.UpdateService(OwningProviderId, ServiceId, UpdateRequest());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Owning_provider_can_delete_its_own_service()
    {
        var mediator = Mediator(owningProviderId: OwningProviderId);
        var controller = BuildController(Principal(OwningProviderId, "Provider"), mediator);

        var result = await controller.DeleteService(OwningProviderId, ServiceId);

        result.Should().BeOfType<NoContentResult>();
    }

    // ---------------------------------------------------------------- the admin path (was 403 for admins too)

    [Theory]
    [InlineData("Admin")]
    [InlineData("SysAdmin")]
    [InlineData("Administrator")]
    public async Task Admins_can_manage_any_service(string role)
    {
        var mediator = Mediator(owningProviderId: OwningProviderId);
        var controller = BuildController(Principal(providerId: null, role), mediator);

        var result = await controller.ActivateService(ServiceId);

        result.Should().BeOfType<OkObjectResult>(
            "an admin owns no provider, so the pre-fix null user id denied them outright");
    }

    // ---------------------------------------------------------------- the hole the fix must NOT open

    [Fact]
    public async Task A_different_provider_cannot_activate_someone_elses_service()
    {
        var mediator = Mediator(owningProviderId: OwningProviderId);
        var controller = BuildController(Principal(OtherProviderId, "Provider"), mediator);

        var result = await controller.ActivateService(ServiceId);

        result.Should().BeOfType<ForbidResult>(
            "cross-tenant service management must stay refused — repairing the claim lookup alone would have " +
            "allowed any provider through");
    }

    [Fact]
    public async Task A_different_provider_cannot_deactivate_someone_elses_service()
    {
        var mediator = Mediator(owningProviderId: OwningProviderId);
        var controller = BuildController(Principal(OtherProviderId, "Provider"), mediator);

        var result = await controller.DeactivateService(ServiceId, request: null);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task A_different_provider_cannot_archive_someone_elses_service()
    {
        var mediator = Mediator(owningProviderId: OwningProviderId);
        var controller = BuildController(Principal(OtherProviderId, "Provider"), mediator);

        var result = await controller.ArchiveService(ServiceId, request: null);

        result.Should().BeOfType<ForbidResult>(
            "archive is destructive; it must never be reachable across tenants");
    }

    [Fact]
    public async Task A_different_provider_cannot_update_someone_elses_service()
    {
        // The vulnerability this pins: UpdateService/DeleteService had no CanManageService
        // check at all (unlike Activate/Deactivate/Archive above) -- only the bare
        // "ProviderOrAdmin" policy, which any provider satisfies. Passing someone else's
        // providerId/serviceId pair here reached the handler with no ownership check.
        var mediator = Mediator(owningProviderId: OwningProviderId);
        var controller = BuildController(Principal(OtherProviderId, "Provider"), mediator);

        var result = await controller.UpdateService(OwningProviderId, ServiceId, UpdateRequest());

        result.Should().BeOfType<ForbidResult>(
            "cross-tenant service editing must be refused -- this returned 200 before the fix");
        // authorization must short-circuit before any state change is dispatched
        await mediator.DidNotReceive().Send(Arg.Any<UpdateProviderServiceCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_different_provider_cannot_delete_someone_elses_service()
    {
        var mediator = Mediator(owningProviderId: OwningProviderId);
        var controller = BuildController(Principal(OtherProviderId, "Provider"), mediator);

        var result = await controller.DeleteService(OwningProviderId, ServiceId);

        result.Should().BeOfType<ForbidResult>(
            "cross-tenant service deletion must be refused -- this returned 204 before the fix");
        // authorization must short-circuit before any state change is dispatched
        await mediator.DidNotReceive().Send(Arg.Any<DeleteProviderServiceCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_different_provider_is_refused_even_without_a_providerId_claim()
    {
        var mediator = Mediator(owningProviderId: OwningProviderId, currentProviderStatusId: OtherProviderId);
        var controller = BuildController(Principal(providerId: null, "Provider"), mediator);

        var result = await controller.ArchiveService(ServiceId, request: null);

        result.Should().BeOfType<ForbidResult>(
            "the in-process ownership fallback must compare providers, not merely prove one exists");
    }

    // ---------------------------------------------------------------- fail-closed edges

    [Fact]
    public async Task A_caller_without_a_user_id_is_refused()
    {
        var mediator = Mediator(owningProviderId: OwningProviderId);
        var controller = BuildController(PrincipalWithoutUserId(), mediator);

        var result = await controller.ActivateService(ServiceId);

        result.Should().BeOfType<ForbidResult>(
            "the helper must still fail closed when the token genuinely carries no user id");
    }

    [Fact]
    public async Task An_unresolvable_service_is_refused_rather_than_throwing()
    {
        var mediator = Mediator(owningProviderId: null);
        var controller = BuildController(Principal(OwningProviderId, "Provider"), mediator);

        var result = await controller.ActivateService(ServiceId);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Ownership_is_never_inferred_from_the_provider_role_alone()
    {
        // The pre-fix implementation's final line was `return User.IsInRole("Provider")`. This pins that a bare
        // Provider role — no matching providerId claim, no resolvable provider — grants nothing.
        var mediator = Mediator(owningProviderId: OwningProviderId);
        var controller = BuildController(Principal(providerId: null, "Provider"), mediator);

        var result = await controller.ArchiveService(ServiceId, request: null);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task A_refused_caller_never_reaches_the_command()
    {
        var mediator = Mediator(owningProviderId: OwningProviderId);
        var controller = BuildController(Principal(OtherProviderId, "Provider"), mediator);

        await controller.ArchiveService(ServiceId, request: null);

        // authorization must short-circuit before any state change is dispatched
        await mediator.DidNotReceive().Send(Arg.Any<ArchiveServiceCommand>(), Arg.Any<CancellationToken>());
    }
}
