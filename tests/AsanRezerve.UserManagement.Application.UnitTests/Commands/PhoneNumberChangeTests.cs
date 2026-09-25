using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Application.Services.Notifications;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Application.CQRS.Commands.SendPhoneVerificationCode;
using AsanRezerve.UserManagement.Application.CQRS.Commands.VerifyPhoneCode;
using AsanRezerve.UserManagement.Domain.Aggregates;
using AsanRezerve.UserManagement.Domain.Aggregates.PhoneVerificationAggregate;
using AsanRezerve.UserManagement.Domain.Entities;
using AsanRezerve.UserManagement.Domain.Enums;
using AsanRezerve.UserManagement.Domain.Repositories;
using AsanRezerve.UserManagement.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AsanRezerve.UserManagement.Application.UnitTests.Commands;

/// <summary>
/// refactor-identity-and-membership §1.8: the two live, [Authorize]-protected endpoints backing
/// the Vue provider dashboard's "PhoneVerificationModal" (<c>POST /users/{id}/phone/send-verification</c>,
/// <c>POST /users/{id}/phone/verify</c>) dispatched <see cref="SendPhoneVerificationCodeCommand"/> /
/// <see cref="VerifyPhoneCodeCommand"/> to handlers that were entirely commented out — every call
/// threw "handler not found". These tests pin the real behavior of the rewritten handlers, which
/// go through the canonical <see cref="PhoneVerification"/> aggregate
/// (<see cref="VerificationPurpose.PhoneNumberChange"/>) instead of resurrecting the old
/// <c>IDistributedCache</c> design.
/// </summary>
public sealed class PhoneNumberChangeTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPhoneVerificationRepository _verificationRepository = Substitute.For<IPhoneVerificationRepository>();
    private readonly ISmsNotificationService _smsService = Substitute.For<ISmsNotificationService>();

    private readonly SendPhoneVerificationCodeCommandHandler _sendHandler;
    private readonly VerifyPhoneCodeCommandHandler _verifyHandler;

    private const string CurrentPhone = "09121111111";
    private const string NewPhone = "09122222222";
    private const string OtherPersonsPhone = "09123333333";

    public PhoneNumberChangeTests()
    {
        _sendHandler = new SendPhoneVerificationCodeCommandHandler(
            _userRepository, _verificationRepository, _smsService,
            Substitute.For<ILogger<SendPhoneVerificationCodeCommandHandler>>());

        _verifyHandler = new VerifyPhoneCodeCommandHandler(
            _userRepository, _verificationRepository,
            Substitute.For<ILogger<VerifyPhoneCodeCommandHandler>>());

        _smsService.SendSmsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, object>?>(), Arg.Any<CancellationToken>())
            .Returns((true, "msg-id", (string?)null));

        // SendPhoneVerificationCodeCommandHandler's rate-limit gate is `#if !DEBUG` (left out of
        // Debug builds so repeated local/CI-Debug test runs don't go flaky on it) — but `dotnet test
        // --configuration Release` (what deploy.yml's "Run Unit Tests" step actually runs) DOES
        // compile it in, and an unstubbed NSubstitute call here returns null, not an empty list —
        // unlike the real EF repository (PhoneVerificationRepository.GetRecentVerificationsByPhoneAsync
        // wraps ToListAsync, which never returns null). Stubbing "no recent verifications" as the
        // default keeps every test in this class correct under both configurations; found when this
        // NullReferenceException broke the Release-config CI run for the first time.
        _verificationRepository.GetRecentVerificationsByPhoneAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new List<PhoneVerification>());
    }

    private static User NewUser(string phone)
    {
        var profile = UserProfile.Create("Test", "Owner", middleName: null, dateOfBirth: null, gender: null);
        return User.RegisterWithPhone(
            Email.Create($"{Guid.NewGuid():N}@asanrezerve.test"),
            PhoneNumber.From(phone),
            profile,
            UserType.Provider);
    }

    private void ArrangeUser(User user)
    {
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
    }

    // ---------------------------------------------------------------- Send

    [Fact]
    public async Task Send_Creates_A_PhoneNumberChange_Verification_And_Sends_An_Sms()
    {
        var user = NewUser(CurrentPhone);
        ArrangeUser(user);
        _userRepository.GetByPhoneNumberAsync(PhoneNumber.From(NewPhone).Value, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sendHandler.Handle(
            new SendPhoneVerificationCodeCommand(user.Id.Value, NewPhone), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        await _verificationRepository.Received(1).AddAsync(
            Arg.Is<PhoneVerification>(v => v.Purpose == VerificationPurpose.PhoneNumberChange
                                            && v.PhoneNumber.Value == PhoneNumber.From(NewPhone).Value),
            Arg.Any<CancellationToken>());
        await _verificationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _smsService.Received(1).SendSmsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, object>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_Allows_Re_Verifying_The_Users_Own_Current_Number()
    {
        var user = NewUser(CurrentPhone);
        ArrangeUser(user);
        _userRepository.GetByPhoneNumberAsync(PhoneNumber.From(CurrentPhone).Value, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sendHandler.Handle(
            new SendPhoneVerificationCodeCommand(user.Id.Value, CurrentPhone), CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Send_Rejects_A_Number_Already_Owned_By_Another_User()
    {
        var user = NewUser(CurrentPhone);
        var someoneElse = NewUser(OtherPersonsPhone);
        ArrangeUser(user);
        _userRepository.GetByPhoneNumberAsync(PhoneNumber.From(OtherPersonsPhone).Value, Arg.Any<CancellationToken>()).Returns(someoneElse);

        Func<Task> act = () => _sendHandler.Handle(
            new SendPhoneVerificationCodeCommand(user.Id.Value, OtherPersonsPhone), CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>();
        await _verificationRepository.DidNotReceive().AddAsync(Arg.Any<PhoneVerification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_Throws_NotFound_For_An_Unknown_User()
    {
        _userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _sendHandler.Handle(
            new SendPhoneVerificationCodeCommand(Guid.NewGuid(), NewPhone), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // -------------------------------------------------------------- Verify

    [Fact]
    public async Task Verify_With_The_Right_Code_Updates_The_Users_Phone_Number()
    {
        var user = NewUser(CurrentPhone);
        ArrangeUser(user);

        var verification = PhoneVerification.Create(
            PhoneNumber.From(NewPhone), VerificationMethod.Sms, VerificationPurpose.PhoneNumberChange, user.Id);
        var realCode = verification.OtpCode.Value;

        _verificationRepository.GetByPhoneAndPurposeAsync(
                PhoneNumber.From(NewPhone).Value, VerificationPurpose.PhoneNumberChange, Arg.Any<CancellationToken>())
            .Returns(verification);
        _userRepository.GetByPhoneNumberAsync(PhoneNumber.From(NewPhone).Value, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _verifyHandler.Handle(
            new VerifyPhoneCodeCommand(user.Id.Value, NewPhone, realCode), CancellationToken.None);

        result.Success.Should().BeTrue();
        user.PhoneNumber!.Value.Should().Be(PhoneNumber.From(NewPhone).Value);
        user.PhoneNumberVerified.Should().BeTrue();
        await _userRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _verificationRepository.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Verify_With_The_Wrong_Code_Does_Not_Change_The_Phone_Number()
    {
        var user = NewUser(CurrentPhone);
        ArrangeUser(user);

        var verification = PhoneVerification.Create(
            PhoneNumber.From(NewPhone), VerificationMethod.Sms, VerificationPurpose.PhoneNumberChange, user.Id);

        _verificationRepository.GetByPhoneAndPurposeAsync(
                PhoneNumber.From(NewPhone).Value, VerificationPurpose.PhoneNumberChange, Arg.Any<CancellationToken>())
            .Returns(verification);

        var result = await _verifyHandler.Handle(
            new VerifyPhoneCodeCommand(user.Id.Value, NewPhone, "000000"), CancellationToken.None);

        result.Success.Should().BeFalse();
        user.PhoneNumber!.Value.Should().Be(PhoneNumber.From(CurrentPhone).Value);
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Verify_With_No_Pending_Verification_Fails_Gracefully()
    {
        var user = NewUser(CurrentPhone);
        ArrangeUser(user);
        _verificationRepository.GetByPhoneAndPurposeAsync(
                Arg.Any<string>(), VerificationPurpose.PhoneNumberChange, Arg.Any<CancellationToken>())
            .Returns((PhoneVerification?)null);

        var result = await _verifyHandler.Handle(
            new VerifyPhoneCodeCommand(user.Id.Value, NewPhone, "123456"), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Verify_Rejects_A_Number_Claimed_By_Someone_Else_Between_Send_And_Verify()
    {
        var user = NewUser(CurrentPhone);
        var claimedBy = NewUser(NewPhone);
        ArrangeUser(user);

        var verification = PhoneVerification.Create(
            PhoneNumber.From(NewPhone), VerificationMethod.Sms, VerificationPurpose.PhoneNumberChange, user.Id);
        var realCode = verification.OtpCode.Value;

        _verificationRepository.GetByPhoneAndPurposeAsync(
                PhoneNumber.From(NewPhone).Value, VerificationPurpose.PhoneNumberChange, Arg.Any<CancellationToken>())
            .Returns(verification);
        _userRepository.GetByPhoneNumberAsync(PhoneNumber.From(NewPhone).Value, Arg.Any<CancellationToken>())
            .Returns(claimedBy);

        var result = await _verifyHandler.Handle(
            new VerifyPhoneCodeCommand(user.Id.Value, NewPhone, realCode), CancellationToken.None);

        result.Success.Should().BeFalse();
        user.PhoneNumber!.Value.Should().Be(PhoneNumber.From(CurrentPhone).Value);
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Verify_Rejects_An_Already_Consumed_Code_Without_Throwing()
    {
        var user = NewUser(CurrentPhone);
        ArrangeUser(user);

        var verification = PhoneVerification.Create(
            PhoneNumber.From(NewPhone), VerificationMethod.Sms, VerificationPurpose.PhoneNumberChange, user.Id);
        var realCode = verification.OtpCode.Value;
        verification.Verify(realCode); // already consumed

        _verificationRepository.GetByPhoneAndPurposeAsync(
                PhoneNumber.From(NewPhone).Value, VerificationPurpose.PhoneNumberChange, Arg.Any<CancellationToken>())
            .Returns(verification);

        Func<Task> act = () => _verifyHandler.Handle(
            new VerifyPhoneCodeCommand(user.Id.Value, NewPhone, realCode), CancellationToken.None);

        await act.Should().NotThrowAsync();
        var result = await _verifyHandler.Handle(
            new VerifyPhoneCodeCommand(user.Id.Value, NewPhone, realCode), CancellationToken.None);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Verify_Throws_NotFound_For_An_Unknown_User()
    {
        _userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _verifyHandler.Handle(
            new VerifyPhoneCodeCommand(Guid.NewGuid(), NewPhone, "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
