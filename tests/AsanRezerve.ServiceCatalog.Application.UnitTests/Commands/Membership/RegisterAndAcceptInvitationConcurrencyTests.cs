using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Identity;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Application.Commands.Membership.RegisterAndAcceptInvitation;
using AsanRezerve.ServiceCatalog.Application.Services.Interfaces;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AsanRezerve.ServiceCatalog.Application.UnitTests.Commands.Membership;

/// <summary>
/// Two register-and-accept requests for the same invitation and brand-new phone: the per-phone
/// advisory lock lets exactly one of them CREATE the person and the other REUSE it, and then both
/// race to insert the membership, which a unique index lets only one win.
///
/// <para>When the creator lost that second race, the handler "compensated" by deleting the account it
/// had just created — which by then was the account the winning request had successfully made a
/// member. Measured: half of all isolated runs of the integration race test ended with the new
/// member's account soft-deleted. 1b422077 had stopped the reusing request from compensating; this
/// is the mirror case it left open.</para>
///
/// <para>The rule these tests pin: once the person row commits it is shared by phone, so no request
/// owns it any more and none may delete it. An account left without a membership is harmless — the
/// next sign-in with that phone reuses it, which is the outcome <c>PersonProvisioningService</c> is
/// designed around.</para>
/// </summary>
public class RegisterAndAcceptInvitationConcurrencyTests
{
    private readonly IProviderInvitationReadRepository _invitationRead = Substitute.For<IProviderInvitationReadRepository>();
    private readonly IProviderInvitationWriteRepository _invitationWrite = Substitute.For<IProviderInvitationWriteRepository>();
    private readonly IOrganizationMembershipRepository _memberships = Substitute.For<IOrganizationMembershipRepository>();
    private readonly IMembershipAuditRepository _audit = Substitute.For<IMembershipAuditRepository>();
    private readonly IPersonDirectory _people = Substitute.For<IPersonDirectory>();
    private readonly IInvitationRegistrationService _registration = Substitute.For<IInvitationRegistrationService>();
    private readonly IServiceCatalogUnitOfWork _unitOfWork = Substitute.For<IServiceCatalogUnitOfWork>();
    private readonly IMemberBookabilityService _bookability = Substitute.For<IMemberBookabilityService>();

    private readonly ProviderInvitation _invitation =
        ProviderInvitation.Create(ProviderId.New(), PhoneNumber.From("+989121110033"));

    public RegisterAndAcceptInvitationConcurrencyTests()
    {
        _invitationRead.GetByIdAsync(_invitation.Id, Arg.Any<CancellationToken>()).Returns(_invitation);
        _registration.VerifyOtpAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _people.FindByPhoneAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((PersonInfo?)null);
        _memberships.GetActiveByPersonAndOrganizationAsync(
                Arg.Any<UserId>(), Arg.Any<ProviderId>(), Arg.Any<CancellationToken>())
            .Returns((OrganizationMembership?)null);
    }

    private RegisterAndAcceptInvitationCommandHandler CreateHandler() => new(
        _invitationRead, _invitationWrite, _memberships, _audit, _people,
        _registration, _unitOfWork, _bookability,
        // Accepting an invitation now notifies the organisation's owner. Substituted rather than
        // exercised: what this class tests is the concurrency of the accept itself, and a notification
        // raised on a stubbed provider lookup would assert nothing.
        Substitute.For<INotificationRaiser>(),
        Substitute.For<IProviderReadRepository>(),
        Substitute.For<ILogger<RegisterAndAcceptInvitationCommandHandler>>());

    private RegisterAndAcceptInvitationCommand Command() =>
        new(_invitation.Id, "Race", "One", Email: null, OtpCode: "123456");

    [Fact]
    public async Task A_creator_that_loses_the_membership_race_does_not_delete_the_shared_account()
    {
        _registration.CreateUserWithPhoneAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CreatedPersonAccount(UserId.CreateNew(), IsNewAccount: true));

        // The other request inserted the membership first; this one's commit hits the unique index.
        var lostRace = new InvalidOperationException(
            "duplicate key value violates unique constraint \"ux_membership_person_org_active\"");
        _unitOfWork.SaveAndPublishEventsAsync(Arg.Any<CancellationToken>()).ThrowsAsync(lostRace);

        var act = () => CreateHandler().Handle(Command(), CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Should().BeSameAs(lostRace, "the failure is still reported to the caller");

        OnlyVerifiedAndProvisioned();
    }

    [Fact]
    public async Task A_reusing_request_that_fails_does_not_delete_the_account_either()
    {
        _registration.CreateUserWithPhoneAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CreatedPersonAccount(UserId.CreateNew(), IsNewAccount: false));
        _unitOfWork.SaveAndPublishEventsAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("lost the race"));

        var act = () => CreateHandler().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        OnlyVerifiedAndProvisioned();
    }

    /// <summary>
    /// The handler may verify the OTP and create-or-reuse the person, and nothing else: in
    /// particular no delete or other undo reaches the person once it exists. Written against the
    /// calls actually received rather than one named method, so a clean-up path reintroduced under
    /// another name fails here too.
    /// </summary>
    private void OnlyVerifiedAndProvisioned()
    {
        _registration.ReceivedCalls()
            .Select(c => c.GetMethodInfo().Name)
            .Distinct()
            .Should().BeEquivalentTo(
                new[] { nameof(IInvitationRegistrationService.VerifyOtpAsync),
                        nameof(IInvitationRegistrationService.CreateUserWithPhoneAsync) },
                "a person committed under the per-phone lock is shared by phone and is no request's to remove");
    }
}
