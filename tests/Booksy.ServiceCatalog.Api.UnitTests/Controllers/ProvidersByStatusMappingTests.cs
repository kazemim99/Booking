using Booksy.ServiceCatalog.API.Controllers.V1;
using Booksy.ServiceCatalog.API.Models.Responses;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetProvidersByStatus;
using Booksy.ServiceCatalog.Application.Services;
using Booksy.ServiceCatalog.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Booksy.ServiceCatalog.Api.UnitTests.Controllers;

/// <summary>
/// Regression cover for the admin provider-queue defect: <c>GET /Providers/by-status/{status}</c> answered
/// <b>500 for every status</b>, which is the only endpoint the admin panel can use to triage the verification queue.
///
/// <para><b>The bug.</b> The controller's response mapper took a <c>dynamic</c> parameter and read
/// <c>provider.Type</c>. The query it serves returns <see cref="ProviderListViewModel"/>, which exposes the category
/// as <c>PrimaryCategory</c> — there is no <c>Type</c> member. Because the parameter was <c>dynamic</c> the compiler
/// could not see the mismatch, so it bound at runtime and threw <c>RuntimeBinderException</c> on the first row.</para>
///
/// <para><b>Why the parameter type matters.</b> The mapper is now statically typed to
/// <see cref="ProviderListViewModel"/>. That is the actual fix: an equivalent mistake becomes a compile error rather
/// than a 500 discovered in production. These tests additionally pin the behaviour so a future refactor back to
/// <c>dynamic</c> (or a renamed view-model member) fails here first.</para>
/// </summary>
public class ProvidersByStatusMappingTests
{
    private static ProvidersController BuildController(ISender mediator) =>
        new(
            mediator,
            NullLogger<ProvidersController>.Instance,
            Substitute.For<IImageStorageService>(),
            Substitute.For<Booksy.ServiceCatalog.Application.Services.Interfaces.ITokenService>());

    private static ProviderListViewModel ViewModel(
        ProviderStatus status = ProviderStatus.PendingVerification,
        ServiceCategory category = ServiceCategory.Barbershop) =>
        new()
        {
            Id = Guid.Parse("a8346c06-b292-46cb-8951-d1f8bf711ed2"),
            BusinessName = "آرایشگاه نهال",
            Description = "آرایشگاه با بیش از ۲۰ سابقه",
            Status = status,
            PrimaryCategory = category,
            City = "اسلامشهر",
            State = "تهران",
            Country = "IR",
            AllowOnlineBooking = true,
            OffersMobileServices = false,
            IsVerified = false,
            AverageRating = 0m,
            TotalReviews = 0,
            ServiceCount = 0,
            RegisteredAt = new DateTime(2026, 8, 11, 16, 45, 55, DateTimeKind.Utc),
        };

    private static ISender Mediator(params ProviderListViewModel[] rows)
    {
        var mediator = Substitute.For<ISender>();

        mediator
            .Send(Arg.Any<GetProvidersByStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ProviderListViewModel>)rows.ToList());

        return mediator;
    }

    private static IReadOnlyList<ProviderSummaryResponse> Payload(IActionResult result)
    {
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        return ok.Value.Should().BeAssignableTo<IReadOnlyList<ProviderSummaryResponse>>().Subject;
    }

    [Fact]
    public async Task Returns_the_queue_instead_of_throwing_on_the_first_row()
    {
        var controller = BuildController(Mediator(ViewModel()));

        var result = await controller.GetProvidersByStatus(ProviderStatus.PendingVerification);

        Payload(result).Should().ContainSingle(
            "reading a non-existent 'Type' member threw RuntimeBinderException, so the endpoint 500'd for every " +
            "status — the admin verification queue was entirely unreachable");
    }

    [Fact]
    public async Task Projects_the_view_model_category_into_the_response_type()
    {
        var controller = BuildController(Mediator(ViewModel(category: ServiceCategory.Barbershop)));

        var result = await controller.GetProvidersByStatus(ProviderStatus.PendingVerification);

        Payload(result).Single().Type.Should().Be("Barbershop",
            "the response's Type is sourced from the view model's PrimaryCategory — the member the defect missed");
    }

    [Theory]
    [InlineData(ProviderStatus.Drafted)]
    [InlineData(ProviderStatus.PendingVerification)]
    [InlineData(ProviderStatus.Verified)]
    [InlineData(ProviderStatus.Active)]
    [InlineData(ProviderStatus.Inactive)]
    [InlineData(ProviderStatus.Suspended)]
    [InlineData(ProviderStatus.Archived)]
    public async Task Round_trips_every_real_status(ProviderStatus status)
    {
        var controller = BuildController(Mediator(ViewModel(status)));

        var result = await controller.GetProvidersByStatus(status);

        Payload(result).Single().Status.Should().Be(status.ToString(),
            "the admin panel filters on these exact strings; a silently dropped or renamed status empties a tab");
    }

    [Fact]
    public async Task Maps_every_row_of_a_multi_row_queue()
    {
        var controller = BuildController(Mediator(ViewModel(), ViewModel(), ViewModel()));

        var result = await controller.GetProvidersByStatus(ProviderStatus.PendingVerification);

        Payload(result).Should().HaveCount(3,
            "the pending count shown on the admin tab is the length of this list");
    }

    [Fact]
    public async Task Returns_an_empty_list_for_a_status_with_no_providers()
    {
        var controller = BuildController(Mediator());

        var result = await controller.GetProvidersByStatus(ProviderStatus.Archived);

        Payload(result).Should().BeEmpty("an empty queue is a normal answer, not an error");
    }

    [Fact]
    public async Task Carries_the_identifying_fields_the_admin_table_renders()
    {
        var controller = BuildController(Mediator(ViewModel()));

        var result = await controller.GetProvidersByStatus(ProviderStatus.PendingVerification);

        var row = Payload(result).Single();
        row.Id.Should().Be(Guid.Parse("a8346c06-b292-46cb-8951-d1f8bf711ed2"));
        row.BusinessName.Should().Be("آرایشگاه نهال", "non-ASCII business names must survive the projection");
        row.City.Should().Be("اسلامشهر");
    }

    [Fact]
    public async Task Forwards_the_requested_status_to_the_query()
    {
        var mediator = Mediator(ViewModel());
        var controller = BuildController(mediator);

        await controller.GetProvidersByStatus(ProviderStatus.Suspended);

        // Each admin tab requests one status; forwarding the wrong one shows the wrong providers.
        await mediator.Received(1).Send(
            Arg.Is<GetProvidersByStatusQuery>(q => q.Status == ProviderStatus.Suspended),
            Arg.Any<CancellationToken>());
    }
}
