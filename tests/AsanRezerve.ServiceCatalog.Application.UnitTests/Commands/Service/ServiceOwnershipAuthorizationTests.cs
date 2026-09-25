using AsanRezerve.Core.Application.Abstractions.Services;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Commands.Service.DeleteProviderService;
using AsanRezerve.ServiceCatalog.Application.Commands.Service.UpdateProviderService;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AsanRezerve.ServiceCatalog.Application.UnitTests.Commands.Service;

/// <summary>
/// Defense-in-depth cover for the two handlers behind ServicesController's Update/Delete
/// routes: the "service belongs to request.ProviderId" check they already had only proves
/// internal consistency, never that the CALLER owns that provider. ServicesController now
/// checks this itself (CanManageService, matching Activate/Deactivate/Archive), but these
/// handlers guard the same invariant independently, so a future caller that forgets the
/// controller-level check still cannot reach another provider's service.
/// </summary>
public class ServiceOwnershipAuthorizationTests
{
    private static Provider Org(UserId owner) => Provider.RegisterProvider(
        owner, "Salon", "desc", ServiceCategory.Barbershop,
        ContactInfo.Create(Email.Create("s@t.com"), PhoneNumber.From("+989123456789")),
        BusinessAddress.Create("a", "b", "c", "d", "12345", "IR"));

    private static Domain.Aggregates.Service Svc(ProviderId providerId) =>
        Domain.Aggregates.Service.Create(
            providerId, "Haircut", "desc", ServiceCategory.Barbershop,
            ServiceType.Standard, Price.Create(100m, "USD"), Duration.FromMinutes(30));

    [Fact]
    public async Task Update_A_different_providers_service_is_forbidden()
    {
        var owner = Org(UserId.From(Guid.NewGuid()));
        var attacker = UserId.From(Guid.NewGuid());
        var service = Svc(owner.Id);

        var serviceWrite = Substitute.For<IServiceWriteRepository>();
        serviceWrite.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var serviceRead = Substitute.For<IServiceReadRepository>();
        var providerRead = Substitute.For<IProviderReadRepository>();
        providerRead.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(attacker.Value.ToString());
        currentUser.IsInRole(Arg.Any<string>()).Returns(false);

        var handler = new UpdateProviderServiceCommandHandler(
            serviceWrite, serviceRead, providerRead, currentUser,
            Substitute.For<ILogger<UpdateProviderServiceCommandHandler>>());

        Func<Task> act = () => handler.Handle(
            new UpdateProviderServiceCommand(
                service.Id.Value, owner.Id.Value, "New Name", null, 0, 30, 100m, "USD", null, false),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>(
            "the caller owns no provider matching request.ProviderId -- this must never fall through to a save");
        await serviceWrite.DidNotReceive().UpdateServiceAsync(Arg.Any<Domain.Aggregates.Service>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_A_different_providers_service_is_forbidden()
    {
        var owner = Org(UserId.From(Guid.NewGuid()));
        var attacker = UserId.From(Guid.NewGuid());
        var service = Svc(owner.Id);

        var serviceWrite = Substitute.For<IServiceWriteRepository>();
        serviceWrite.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var bookingRead = Substitute.For<IBookingReadRepository>();
        var providerRead = Substitute.For<IProviderReadRepository>();
        providerRead.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(attacker.Value.ToString());
        currentUser.IsInRole(Arg.Any<string>()).Returns(false);

        var handler = new DeleteProviderServiceCommandHandler(
            serviceWrite, bookingRead, providerRead, currentUser,
            Substitute.For<ILogger<DeleteProviderServiceCommandHandler>>());

        Func<Task> act = () => handler.Handle(
            new DeleteProviderServiceCommand(service.Id.Value, owner.Id.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await serviceWrite.DidNotReceive().DeleteServiceAsync(Arg.Any<Domain.Aggregates.Service>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_By_An_Admin_Is_Allowed_Regardless_Of_Ownership()
    {
        var owner = Org(UserId.From(Guid.NewGuid()));
        var service = Svc(owner.Id);

        var serviceWrite = Substitute.For<IServiceWriteRepository>();
        serviceWrite.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var serviceRead = Substitute.For<IServiceReadRepository>();
        var providerRead = Substitute.For<IProviderReadRepository>();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid().ToString());
        currentUser.IsInRole("Admin").Returns(true);

        var handler = new UpdateProviderServiceCommandHandler(
            serviceWrite, serviceRead, providerRead, currentUser,
            Substitute.For<ILogger<UpdateProviderServiceCommandHandler>>());

        var result = await handler.Handle(
            new UpdateProviderServiceCommand(
                service.Id.Value, owner.Id.Value, "New Name", null, 0, 30, 100m, "USD", null, false),
            CancellationToken.None);

        result.ServiceName.Should().Be("New Name");
        // The admin bypass must not even need to resolve the provider.
        await providerRead.DidNotReceive().GetByIdAsync(Arg.Any<ProviderId>(), Arg.Any<CancellationToken>());
    }
}
