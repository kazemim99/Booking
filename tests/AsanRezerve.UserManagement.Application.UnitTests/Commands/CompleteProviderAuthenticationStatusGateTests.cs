using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Application.Abstractions.Persistence;
using AsanRezerve.UserManagement.Application.CQRS.Commands.CompleteProviderAuthentication;
using AsanRezerve.UserManagement.Application.CQRS.Commands.VerifyPhone;
using AsanRezerve.UserManagement.Application.Services.Interfaces;
using AsanRezerve.UserManagement.Domain.Aggregates;
using AsanRezerve.UserManagement.Domain.Aggregates.PhoneVerificationAggregate;
using AsanRezerve.UserManagement.Domain.Entities;
using AsanRezerve.UserManagement.Domain.Enums;
using AsanRezerve.UserManagement.Domain.Repositories;
using AsanRezerve.UserManagement.Domain.Services;
using AsanRezerve.UserManagement.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AsanRezerve.UserManagement.Application.UnitTests.Commands;

/// <summary>
/// Regression tests for the OTP status gate: the passwordless (OTP) provider sign-in
/// path issues tokens directly, so it must reject Banned/Suspended/Inactive accounts
/// itself (the password path relies on User.Authenticate(), which OTP bypasses).
/// Before the fix, a blocked user could obtain a fresh JWT via OTP.
/// </summary>
public class CompleteProviderAuthenticationStatusGateTests
{
    private readonly ISender _mediator = Substitute.For<ISender>();
    private readonly IPhoneVerificationRepository _verificationRepo = Substitute.For<IPhoneVerificationRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IProviderInfoService _providerInfoService = Substitute.For<IProviderInfoService>();
    private readonly IMembershipInfoService _membershipInfoService = Substitute.For<IMembershipInfoService>();
    private readonly IUserManagementUnitOfWork _unitOfWork = Substitute.For<IUserManagementUnitOfWork>();
    private readonly IPersonProvisioningService _personProvisioning = Substitute.For<IPersonProvisioningService>();
    private readonly CompleteProviderAuthenticationCommandHandler _handler;

    private const string Phone = "+989121234567";

    public CompleteProviderAuthenticationStatusGateTests()
    {
        _handler = new CompleteProviderAuthenticationCommandHandler(
            _mediator,
            _verificationRepo,
            _userRepository,
            _jwtTokenService,
            _providerInfoService,
            _membershipInfoService,
            _personProvisioning,
            _unitOfWork,
            Substitute.For<ILogger<CompleteProviderAuthenticationCommandHandler>>());
    }

    private static User ProviderUser(UserStatus status)
    {
        var profile = UserProfile.Create("Test", "Provider", middleName: null, dateOfBirth: null, gender: null);
        var user = User.RegisterWithPhone(
            Email.Create("t@asanrezerve.provider"),
            PhoneNumber.From(Phone),
            profile,
            UserType.Provider);
        user.SetStatus(status);
        return user;
    }

    private void ArrangeVerifiedOtp()
    {
        var verification = PhoneVerification.Create(
            PhoneNumber.From(Phone), VerificationMethod.Sms, VerificationPurpose.Registration);
        _verificationRepo.GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(verification);
        _mediator.Send(Arg.Any<VerifyPhoneCommand>(), Arg.Any<CancellationToken>())
            .Returns(new VerifyPhoneResult(true, "verified", Phone));
    }

    [Theory]
    [InlineData(UserStatus.Banned)]
    [InlineData(UserStatus.Suspended)]
    [InlineData(UserStatus.Inactive)]
    public async Task Rejects_OTP_SignIn_For_A_Blocked_Account(UserStatus status)
    {
        // Arrange
        ArrangeVerifiedOtp();
        var person = ProviderUser(status);
        _personProvisioning.GetOrCreateByPhoneAsync(
                Arg.Any<PhoneNumber>(), Arg.Any<UserType>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new PersonProvisioningResult(person, IsNewPerson: false, CapacityGranted: false));

        var command = new CompleteProviderAuthenticationCommand { PhoneNumber = Phone, Code = "123456" };

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert — rejected before any token is minted or persisted.
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{status}*");
        _jwtTokenService.ReceivedCalls().Should().BeEmpty();
        await _unitOfWork.DidNotReceive().SaveAndPublishEventsAsync(Arg.Any<CancellationToken>());
    }
}
