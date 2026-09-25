using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Application.Services.Notifications;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Commands.Membership.SendInvitationOtp;
using AsanRezerve.ServiceCatalog.Application.Services.Interfaces;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AsanRezerve.ServiceCatalog.Application.UnitTests.Commands.Membership;

public class SendInvitationOtpCommandHandlerTests
{
    private readonly IProviderInvitationReadRepository _invitationReadRepository =
        Substitute.For<IProviderInvitationReadRepository>();
    private readonly IInvitationRegistrationService _registrationService =
        Substitute.For<IInvitationRegistrationService>();
    private readonly ISmsNotificationService _smsService =
        Substitute.For<ISmsNotificationService>();

    private SendInvitationOtpCommandHandler CreateHandler() => new(
        _invitationReadRepository,
        _registrationService,
        _smsService,
        Substitute.For<ILogger<SendInvitationOtpCommandHandler>>());

    private static ProviderInvitation PendingInvitation(string phone = "+989121234567") =>
        ProviderInvitation.Create(ProviderId.New(), PhoneNumber.From(phone), inviteeName: "Test Person");

    [Fact]
    public async Task Sends_The_Generated_Code_By_Sms_And_Returns_The_Masked_Phone()
    {
        var invitation = PendingInvitation("+989121234567");
        _invitationReadRepository.GetByIdAsync(invitation.Id, Arg.Any<CancellationToken>())
            .Returns(invitation);
        _registrationService.GenerateOtpCodeAsync(invitation.PhoneNumber.Value, Arg.Any<CancellationToken>())
            .Returns("135790");
        _smsService.SendSmsAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, object>?>(), Arg.Any<CancellationToken>())
            .Returns((true, "msg-1", (string?)null));

        var handler = CreateHandler();
        var result = await handler.Handle(new SendInvitationOtpCommand(invitation.Id), CancellationToken.None);

        result.MaskedPhoneNumber.Should().Be(new string('•', invitation.PhoneNumber.Value.Length - 4) + "4567");
        await _smsService.Received(1).SendSmsAsync(
            invitation.PhoneNumber.Value,
            Arg.Is<string>(m => m.Contains("135790")),
            Arg.Any<Dictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Never_Leaks_The_Real_Phone_Number_In_The_Result()
    {
        var invitation = PendingInvitation("+989121234567");
        _invitationReadRepository.GetByIdAsync(invitation.Id, Arg.Any<CancellationToken>())
            .Returns(invitation);
        _registrationService.GenerateOtpCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("135790");
        _smsService.SendSmsAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, object>?>(), Arg.Any<CancellationToken>())
            .Returns((true, "msg-1", (string?)null));

        var handler = CreateHandler();
        var result = await handler.Handle(new SendInvitationOtpCommand(invitation.Id), CancellationToken.None);

        result.MaskedPhoneNumber.Should().NotContain(invitation.PhoneNumber.Value);
    }

    [Fact]
    public async Task Throws_When_Invitation_Not_Found()
    {
        _invitationReadRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ProviderInvitation?)null);

        var handler = CreateHandler();
        Func<Task> act = () => handler.Handle(new SendInvitationOtpCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_When_Invitation_Is_Not_Pending()
    {
        var invitation = PendingInvitation();
        invitation.Reject();
        _invitationReadRepository.GetByIdAsync(invitation.Id, Arg.Any<CancellationToken>())
            .Returns(invitation);

        var handler = CreateHandler();
        Func<Task> act = () => handler.Handle(new SendInvitationOtpCommand(invitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>();
        await _smsService.DidNotReceive().SendSmsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, object>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_When_The_Sms_Gateway_Fails()
    {
        var invitation = PendingInvitation();
        _invitationReadRepository.GetByIdAsync(invitation.Id, Arg.Any<CancellationToken>())
            .Returns(invitation);
        _registrationService.GenerateOtpCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("135790");
        _smsService.SendSmsAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, object>?>(), Arg.Any<CancellationToken>())
            .Returns((false, (string?)null, "gateway unavailable"));

        var handler = CreateHandler();
        Func<Task> act = () => handler.Handle(new SendInvitationOtpCommand(invitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
