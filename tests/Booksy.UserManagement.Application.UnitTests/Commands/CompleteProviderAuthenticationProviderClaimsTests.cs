using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Application.Abstractions.Persistence;
using Booksy.UserManagement.Application.CQRS.Commands.CompleteProviderAuthentication;
using Booksy.UserManagement.Application.CQRS.Commands.VerifyPhone;
using Booksy.UserManagement.Application.Services.Interfaces;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Aggregates.PhoneVerificationAggregate;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.Services;
using Booksy.UserManagement.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Booksy.UserManagement.Application.UnitTests.Commands;

/// <summary>
/// Regression tests for provider-claim resolution during OTP sign-in.
///
/// <para>Two defects met here. The provider lookup (<see cref="IProviderInfoService"/>) was an
/// HTTP call to ServiceCatalog's <c>by-owner</c> endpoint, which sits behind the host-wide
/// authenticated-user fallback policy; sign-in completion is the only caller and is by
/// definition not yet authenticated, so it always 401'd. The exception was swallowed as a
/// warning, so every sign-in silently reported "no provider profile": no <c>providerId</c> or
/// <c>providerStatus</c> in the issued token, and <c>RequiresOnboarding</c> left at its
/// initial <c>true</c> — fully-registered providers were sent back through onboarding.</para>
///
/// <para>Fixing the lookup exposed the second defect: <c>RequiresOnboarding</c> was computed as
/// <c>providerStatus == "Pending"</c>, and "Pending" is not a member of ServiceCatalog's
/// ProviderStatus (Drafted, PendingVerification, Verified, Active, Inactive, Suspended,
/// Archived). It could never be true, so once the lookup started succeeding a half-finished
/// <c>Drafted</c> provider would have been told onboarding was complete. Onboarding is now
/// keyed off <c>Drafted</c> alone, matching what the provider app already does client-side
/// (ProviderStatus.needsOnboarding) precisely because this flag was untrustworthy —
/// AUTH_SPECIFICATION.md BUG-1.</para>
/// </summary>
public class CompleteProviderAuthenticationProviderClaimsTests
{
    private readonly ISender _mediator = Substitute.For<ISender>();
    private readonly IPhoneVerificationRepository _verificationRepo = Substitute.For<IPhoneVerificationRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IProviderInfoService _providerInfoService = Substitute.For<IProviderInfoService>();
    private readonly IUserManagementUnitOfWork _unitOfWork = Substitute.For<IUserManagementUnitOfWork>();
    private readonly IPersonProvisioningService _personProvisioning = Substitute.For<IPersonProvisioningService>();
    private readonly CompleteProviderAuthenticationCommandHandler _handler;

    private const string Phone = "+989121234567";

    public CompleteProviderAuthenticationProviderClaimsTests()
    {
        _handler = new CompleteProviderAuthenticationCommandHandler(
            _mediator,
            _verificationRepo,
            _userRepository,
            _jwtTokenService,
            _providerInfoService,
            _personProvisioning,
            _unitOfWork,
            Substitute.For<ILogger<CompleteProviderAuthenticationCommandHandler>>());
    }

    /// <summary>
    /// An existing provider signing in must receive their provider identity and must NOT be
    /// pushed back into onboarding. Covers the statuses that all mean "registration finished":
    /// the reported break was an Active provider, and PendingVerification is the state a
    /// freshly-completed registration sits in while awaiting admin verification.
    /// </summary>
    [Theory]
    [InlineData("Active")]
    [InlineData("PendingVerification")]
    [InlineData("Verified")]
    public async Task An_Existing_Provider_Receives_ProviderId_And_Does_Not_Require_Onboarding(
        string providerStatus)
    {
        // Arrange
        var providerId = Guid.NewGuid();
        ArrangeSignIn(ProviderUser(), new ProviderInfo(providerId, providerStatus));

        // Act
        var result = await _handler.Handle(Command(), CancellationToken.None);

        // Assert
        result.ProviderId.Should().Be(providerId);
        result.ProviderStatus.Should().Be(providerStatus);
        result.RequiresOnboarding.Should().BeFalse(
            "registration is complete for {0}; routing the provider back to onboarding is the reported bug",
            providerStatus);
    }

    /// <summary>
    /// The claims must actually reach the token, not just the response body — the provider app
    /// authorizes subsequent calls with the JWT, so a missing provider_id claim breaks it even
    /// when the response looks correct.
    /// </summary>
    [Fact]
    public async Task An_Existing_Provider_Has_Their_Claims_Minted_Into_The_Access_Token()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        ArrangeSignIn(ProviderUser(), new ProviderInfo(providerId, "Active"));

        // Act
        await _handler.Handle(Command(), CancellationToken.None);

        // Assert
        _jwtTokenService.Received(1).GenerateAccessToken(
            userId: Arg.Any<UserId>(),
            userType: Arg.Any<UserType>(),
            email: Arg.Any<Email>(),
            displayName: Arg.Any<string>(),
            firstName: Arg.Any<string>(),
            lastName: Arg.Any<string>(),
            status: Arg.Any<string>(),
            roles: Arg.Any<IEnumerable<string>>(),
            providerId: providerId.ToString(),
            providerStatus: "Active",
            customerId: Arg.Any<string?>(),
            phoneNumber: Arg.Any<string?>(),
            expirationHours: Arg.Any<int>());
    }

    /// <summary>
    /// Guards the regression that fixing the lookup would otherwise have introduced: a Drafted
    /// provider abandoned mid-registration still owes onboarding, and must not be handed a
    /// dashboard with no services or working hours.
    /// </summary>
    [Fact]
    public async Task A_Drafted_Provider_Still_Requires_Onboarding()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        ArrangeSignIn(ProviderUser(), new ProviderInfo(providerId, "Drafted"));

        // Act
        var result = await _handler.Handle(Command(), CancellationToken.None);

        // Assert
        result.ProviderId.Should().Be(providerId, "the draft exists and must be resumed, not recreated");
        result.RequiresOnboarding.Should().BeTrue();
    }

    /// <summary>
    /// No provider profile at all (a brand-new provider, or a customer appearing here for the
    /// first time) keeps the safe default: no provider claims, onboarding required.
    /// </summary>
    [Fact]
    public async Task A_Person_Without_A_Provider_Profile_Requires_Onboarding()
    {
        // Arrange
        ArrangeSignIn(ProviderUser(), providerInfo: null);

        // Act
        var result = await _handler.Handle(Command(), CancellationToken.None);

        // Assert
        result.ProviderId.Should().BeNull();
        result.ProviderStatus.Should().BeNull();
        result.RequiresOnboarding.Should().BeTrue();
    }

    /// <summary>
    /// The lookup must not be able to fail the sign-in. It is a best-effort enrichment: if
    /// ServiceCatalog throws, the person is still authenticated (without provider claims)
    /// rather than seeing a 500.
    /// </summary>
    [Fact]
    public async Task A_Failing_Provider_Lookup_Does_Not_Fail_The_Sign_In()
    {
        // Arrange
        ArrangeSignIn(ProviderUser(), providerInfo: null);
        _providerInfoService
            .GetProviderByOwnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<Task<ProviderInfo?>>(_ => throw new InvalidOperationException("catalog unavailable"));

        // Act
        var result = await _handler.Handle(Command(), CancellationToken.None);

        // Assert
        result.AccessToken.Should().NotBeNull();
        result.ProviderId.Should().BeNull();
        result.RequiresOnboarding.Should().BeTrue();
    }

    // ---- helpers ----

    private static CompleteProviderAuthenticationCommand Command() =>
        new() { PhoneNumber = Phone, Code = "123456" };

    private static User ProviderUser()
    {
        var profile = UserProfile.Create("Test", "Provider", middleName: null, dateOfBirth: null, gender: null);
        var user = User.RegisterWithPhone(
            Email.Create("t@booksy.provider"),
            PhoneNumber.From(Phone),
            profile,
            UserType.Provider);
        user.SetStatus(UserStatus.Active);
        return user;
    }

    /// <summary>
    /// A verified OTP plus a resolved person, with the provider lookup stubbed to
    /// <paramref name="providerInfo"/> (null meaning "no provider profile").
    /// </summary>
    private void ArrangeSignIn(User person, ProviderInfo? providerInfo)
    {
        var verification = PhoneVerification.Create(
            PhoneNumber.From(Phone), VerificationMethod.Sms, VerificationPurpose.Registration);
        _verificationRepo.GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(verification);
        _mediator.Send(Arg.Any<VerifyPhoneCommand>(), Arg.Any<CancellationToken>())
            .Returns(new VerifyPhoneResult(true, "verified", Phone));

        _personProvisioning.GetOrCreateByPhoneAsync(
                Arg.Any<PhoneNumber>(), Arg.Any<UserType>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new PersonProvisioningResult(person, IsNewPerson: false, CapacityGranted: true));

        _providerInfoService
            .GetProviderByOwnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(providerInfo);

        _jwtTokenService.GenerateAccessToken(
                Arg.Any<UserId>(), Arg.Any<UserType>(), Arg.Any<Email>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<int>())
            .Returns("jwt");
    }
}
