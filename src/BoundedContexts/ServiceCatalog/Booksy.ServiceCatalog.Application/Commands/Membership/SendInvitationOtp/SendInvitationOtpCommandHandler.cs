using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Application.Services.Notifications;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Membership.SendInvitationOtp;

public sealed class SendInvitationOtpCommandHandler
    : ICommandHandler<SendInvitationOtpCommand, SendInvitationOtpResult>
{
    private readonly IProviderInvitationReadRepository _invitationReadRepository;
    private readonly IInvitationRegistrationService _registrationService;
    private readonly ISmsNotificationService _smsService;
    private readonly ILogger<SendInvitationOtpCommandHandler> _logger;

    public SendInvitationOtpCommandHandler(
        IProviderInvitationReadRepository invitationReadRepository,
        IInvitationRegistrationService registrationService,
        ISmsNotificationService smsService,
        ILogger<SendInvitationOtpCommandHandler> logger)
    {
        _invitationReadRepository = invitationReadRepository;
        _registrationService = registrationService;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task<SendInvitationOtpResult> Handle(
        SendInvitationOtpCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationReadRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new NotFoundException($"Invitation with ID {request.InvitationId} not found");

        if (invitation.Status != InvitationStatus.Pending || !invitation.IsValid())
            throw new DomainValidationException(
                $"Invitation is no longer pending or has expired (status: {invitation.Status})");

        var phone = invitation.PhoneNumber.Value;
        var code = await _registrationService.GenerateOtpCodeAsync(phone, cancellationToken);

        var message = $"کد تایید بوکسی شما: {code}\nاین کد تا چند دقیقه دیگر معتبر است.";
        var (success, messageId, errorMessage) = await _smsService.SendSmsAsync(
            phone,
            message,
            metadata: new Dictionary<string, object>
            {
                ["InvitationId"] = invitation.Id,
                ["Purpose"] = "InvitationRegistrationOtp"
            },
            cancellationToken);

        if (!success)
        {
            _logger.LogError(
                "Failed to send invitation OTP. InvitationId: {InvitationId}, Error: {Error}",
                invitation.Id, errorMessage);
            throw new InvalidOperationException("Failed to send verification code. Please try again later.");
        }

        _logger.LogInformation(
            "Invitation OTP sent. InvitationId: {InvitationId}, MessageId: {MessageId}",
            invitation.Id, messageId);

        return new SendInvitationOtpResult(Mask(phone));
    }

    // Show only the last 4 digits; everything else is a bullet — matches
    // GetInvitationSummaryQueryHandler's masking so the accept screen and this
    // step never disagree about what "the masked phone" looks like.
    private static string Mask(string phone)
    {
        if (string.IsNullOrEmpty(phone))
            return string.Empty;
        if (phone.Length <= 4)
            return new string('•', phone.Length);
        return new string('•', phone.Length - 4) + phone[^4..];
    }
}
