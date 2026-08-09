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
            _personProvisioning,
            _unitOfWork,
            Substitute.For<ILogger<CompleteProviderAuthenticationCommandHandler>>());
    }

    private static User ProviderUser(UserStatus status)
    {
        var profile = UserProfile.Create("Test", "Provider", middleName: null, dateOfBirth: null, gender: null);
        var user = User.RegisterWithPhone(
            Email.Create("t@booksy.provider"),
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
