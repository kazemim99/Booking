using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Identity;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProviderFull;
using Booksy.ServiceCatalog.Application.Commands.Provider.Registration;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates.MembershipAuditAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

// Both namespaces declare TimeSlotDto/BreakTimeDto/ServiceDto. RegisterProviderFullCommand's
// BusinessHours use the Registration ones (via DayHoursDto), its Services the local ones.
using RegTimeSlot = Booksy.ServiceCatalog.Application.Commands.Provider.Registration.TimeSlotDto;
using RegBreak = Booksy.ServiceCatalog.Application.Commands.Provider.Registration.BreakTimeDto;
using FullService = Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProviderFull.ServiceDto;

namespace Booksy.ServiceCatalog.Application.UnitTests.Commands.ProviderRegistration;

/// <summary>
/// The owner membership is the durable anchor of the membership model: ownership is
/// expressed as a membership carrying <see cref="MembershipRole.Owner"/>, not only by
/// <c>Provider.OwnerId</c>.
///
/// <para>The step-9 wizard path (<c>SaveStep9CompleteCommandHandler</c>) already created one.
/// <c>register-full</c> — the one-shot path used by the Vue admin and the keystone E2E — did
/// not, so those salons had an owner who was invisible to every membership-based read
/// (<c>/memberships/me</c>, the roster, owner role checks) until someone happened to call
/// <c>/registration/owner-provides-services</c>. These tests pin the fix.</para>
///
/// <para>They also pin the team-member behaviour: the handler's loop used to end in a
/// commented-out <c>provider.AddStaff(...)</c>, so <c>teamMembers</c> were accepted, counted
/// in the response, and silently discarded.</para>
/// </summary>
public class RegisterProviderFullMembershipTests
{
    private readonly IProviderWriteRepository _providerWrite = Substitute.For<IProviderWriteRepository>();
    private readonly IProviderReadRepository _providerRead = Substitute.For<IProviderReadRepository>();
    private readonly IServiceWriteRepository _serviceWrite = Substitute.For<IServiceWriteRepository>();
    private readonly IProviderRegistrationService _registrationService = Substitute.For<IProviderRegistrationService>();
    private readonly IOrganizationMembershipRepository _memberships = Substitute.For<IOrganizationMembershipRepository>();
    private readonly IMembershipAuditRepository _audit = Substitute.For<IMembershipAuditRepository>();
    private readonly IPersonDirectory _personDirectory = Substitute.For<IPersonDirectory>();
    private readonly IMemberBookabilityService _bookability = Substitute.For<IMemberBookabilityService>();
    private readonly IServiceCatalogUnitOfWork _unitOfWork = Substitute.For<IServiceCatalogUnitOfWork>();

    private readonly List<OrganizationMembership> _saved = new();

    private RegisterProviderFullCommandHandler CreateHandler()
    {
        _memberships
            .WhenForAnyArgs(r => r.SaveAsync(default!, default))
            .Do(call => _saved.Add(call.Arg<OrganizationMembership>()));

        // A substituted IConfiguration returns "" for GetValue<bool>, which throws on convert.
        // The handler reads ServiceCatalog:AutoApproveProviders (default true).
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceCatalog:AutoApproveProviders"] = "true"
            })
            .Build();

        return new RegisterProviderFullCommandHandler(
            _providerWrite,
            _providerRead,
            _serviceWrite,
            _registrationService,
            _memberships,
            _audit,
            _personDirectory,
            _bookability,
            _unitOfWork,
            configuration,
            Substitute.For<ILogger<RegisterProviderFullCommandHandler>>());
    }

    private static RegisterProviderFullCommand Command(Guid ownerId, params TeamMemberDto[] team) =>
        new(
            OwnerId: ownerId,
            CategoryId: "Barbershop",
            BusinessInfo: new BusinessInfoDto("Ali Salon", "Ali", "Owner", "+989121234567"),
            Address: new AddressDto("Street 1", null, "Tehran", "1234567890"),
            Location: new LocationDto(35.7, 51.4, "Tehran"),
            BusinessHours: new Dictionary<int, DayHoursDto?>
            {
                [1] = new DayHoursDto(1, true, new RegTimeSlot(9, 0), new RegTimeSlot(18, 0), new List<RegBreak>())
            },
            Services: new List<FullService> { new("Haircut", 0, 45, 250000m, "fixed") },
            AssistanceOptions: new List<string>(),
            TeamMembers: team.ToList());

    [Fact]
    public async Task Registering_A_Salon_Creates_An_Owner_Membership()
    {
        var ownerId = Guid.NewGuid();
        var handler = CreateHandler();

        var result = await handler.Handle(Command(ownerId), CancellationToken.None);

        var owner = _saved.Should().ContainSingle(m => m.IsOwner).Subject;
        owner.PersonId!.Value.Should().Be(ownerId);
        owner.OrganizationId.Value.Should().Be(result.ProviderId);
        owner.Status.Should().Be(MembershipStatus.Active);
    }

    [Fact]
    public async Task The_Owner_Does_Not_Become_A_Bookable_Service_Provider_By_Default()
    {
        // Owning a salon and working in it are separate concepts: the owner opts in
        // separately via SetOwnerProvidesServices, which adds StaffProvider + StaffProfile.
        var handler = CreateHandler();

        await handler.Handle(Command(Guid.NewGuid()), CancellationToken.None);

        var owner = _saved.Single(m => m.IsOwner);
        owner.Roles.Should().BeEquivalentTo(new[] { MembershipRole.Owner });
        owner.ProvidesServices.Should().BeFalse();
        owner.StaffProfile.Should().BeNull();
    }

    [Fact]
    public async Task A_Team_Member_Without_A_Known_Phone_Becomes_An_Unclaimed_Bookable_Member()
    {
        _personDirectory.FindByPhoneAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PersonInfo?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            Command(Guid.NewGuid(), new TeamMemberDto("Sara Stylist", "", "09121110000", "+98", "Stylist", false)),
            CancellationToken.None);

        result.StaffCount.Should().Be(1);
        var member = _saved.Should().ContainSingle(m => !m.IsOwner).Subject;
        member.IsUnclaimed.Should().BeTrue();
        member.StaffProfile!.DisplayName.Should().Be("Sara Stylist");
        await _bookability.Received(1).SyncAsync(member, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_Team_Member_Whose_Phone_Is_A_Known_Person_Is_Linked_To_That_Person()
    {
        var personId = Guid.NewGuid();
        _personDirectory.FindByPhoneAsync("09121110000", Arg.Any<CancellationToken>())
            .Returns(new PersonInfo(personId, "Sara", "Stylist", "+989121110000", null));
        var handler = CreateHandler();

        await handler.Handle(
            Command(Guid.NewGuid(), new TeamMemberDto("Sara Stylist", "", "09121110000", "+98", "Stylist", false)),
            CancellationToken.None);

        var member = _saved.Single(m => !m.IsOwner);
        member.IsUnclaimed.Should().BeFalse();
        member.PersonId!.Value.Should().Be(personId);
        member.Status.Should().Be(MembershipStatus.Active);
        member.ProvidesServices.Should().BeTrue();
    }

    [Fact]
    public async Task A_Team_Member_Who_Is_Also_The_Owner_Does_Not_Get_A_Second_Membership()
    {
        // Would otherwise violate ux_membership_person_org_active.
        var ownerId = Guid.NewGuid();
        _personDirectory.FindByPhoneAsync("09121234567", Arg.Any<CancellationToken>())
            .Returns(new PersonInfo(ownerId, "Ali", "Owner", "+989121234567", null));
        var handler = CreateHandler();

        await handler.Handle(
            Command(ownerId, new TeamMemberDto("Ali Owner", "", "09121234567", "+98", "Owner", false)),
            CancellationToken.None);

        _saved.Should().ContainSingle();
        _saved.Single().IsOwner.Should().BeTrue();
    }
}
