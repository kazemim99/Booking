using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Application.Commands.Provider.CompleteProviderRegistration;
using Booksy.ServiceCatalog.Application.Commands.Provider.Registration;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Booksy.ServiceCatalog.Application.UnitTests.Commands.ProviderRegistration;

/// <summary>
/// None of these five registration-step handlers had any test at all before -- each threw
/// UnauthorizedAccessException (401) for "you are not this provider's owner", which is a
/// permission refusal (403), not a missing-authentication one. Each already computes
/// ownership correctly (provider.OwnerId != userId); only the exception type was wrong.
/// One test per handler, pinning the corrected status at the handler boundary.
/// </summary>
public class ProviderRegistrationAuthorizationTests
{
    private static Provider Draft(UserId owner) => Provider.RegisterProvider(
        owner, "Salon", "desc", ServiceCategory.Barbershop,
        ContactInfo.Create(Email.Create("s@t.com"), PhoneNumber.From("+989123456789")),
        BusinessAddress.Create("a", "b", "c", "d", "12345", "IR"));

    private static ICurrentUserService CurrentUserAs(Guid userId)
    {
        var svc = Substitute.For<ICurrentUserService>();
        svc.UserId.Returns(userId.ToString());
        return svc;
    }

    [Fact]
    public async Task CompleteProviderRegistration_A_Stranger_Is_Forbidden()
    {
        var provider = Draft(UserId.From(Guid.NewGuid()));
        var providerRepo = Substitute.For<IProviderWriteRepository>();
        providerRepo.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);

        var handler = new CompleteProviderRegistrationCommandHandler(
            providerRepo,
            Substitute.For<IServiceCatalogUnitOfWork>(),
            CurrentUserAs(Guid.NewGuid()));

        Func<Task> act = () => handler.Handle(
            new CompleteProviderRegistrationCommand(provider.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>(
            "authenticated but not this provider's owner must be 403, not 401");
    }

    [Fact]
    public async Task SaveStep4Services_A_Stranger_Is_Forbidden()
    {
        var provider = Draft(UserId.From(Guid.NewGuid()));
        var providerRepo = Substitute.For<IProviderWriteRepository>();
        providerRepo.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);

        var handler = new SaveStep4ServicesCommandHandler(
            providerRepo,
            Substitute.For<IServiceWriteRepository>(),
            Substitute.For<IServiceReadRepository>(),
            Substitute.For<IServiceCatalogUnitOfWork>(),
            CurrentUserAs(Guid.NewGuid()));

        Func<Task> act = () => handler.Handle(
            new SaveStep4ServicesCommand(provider.Id.Value, new List<ServiceDto>()), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task SaveStep6WorkingHours_A_Stranger_Is_Forbidden()
    {
        var provider = Draft(UserId.From(Guid.NewGuid()));
        var providerRepo = Substitute.For<IProviderWriteRepository>();
        providerRepo.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);

        var handler = new SaveStep6WorkingHoursCommandHandler(
            providerRepo,
            Substitute.For<IServiceCatalogUnitOfWork>(),
            CurrentUserAs(Guid.NewGuid()));

        Func<Task> act = () => handler.Handle(
            new SaveStep6WorkingHoursCommand(provider.Id.Value, new List<DayHoursDto>()), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task SaveStep8Feedback_A_Stranger_Is_Forbidden()
    {
        var provider = Draft(UserId.From(Guid.NewGuid()));
        var providerRepo = Substitute.For<IProviderWriteRepository>();
        providerRepo.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);

        var handler = new SaveStep8FeedbackCommandHandler(
            providerRepo,
            Substitute.For<IServiceCatalogUnitOfWork>(),
            CurrentUserAs(Guid.NewGuid()));

        Func<Task> act = () => handler.Handle(
            new SaveStep8FeedbackCommand(provider.Id.Value, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task SaveStep9Complete_A_Stranger_Is_Forbidden()
    {
        var provider = Draft(UserId.From(Guid.NewGuid()));
        var providerRepo = Substitute.For<IProviderWriteRepository>();
        providerRepo.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);

        var handler = new SaveStep9CompleteCommandHandler(
            providerRepo,
            Substitute.For<IServiceWriteRepository>(),
            Substitute.For<IOrganizationMembershipRepository>(),
            Substitute.For<IServiceCatalogUnitOfWork>(),
            CurrentUserAs(Guid.NewGuid()),
            Substitute.For<ITokenService>(),
            Substitute.For<ILogger<SaveStep9CompleteCommandHandler>>());

        Func<Task> act = () => handler.Handle(
            new SaveStep9CompleteCommand(provider.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
