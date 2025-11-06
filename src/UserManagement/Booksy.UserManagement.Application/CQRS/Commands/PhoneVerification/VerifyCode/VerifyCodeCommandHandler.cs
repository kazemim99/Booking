using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.UserManagement.Application.Commands.PhoneVerification.VerifyPhone;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Commands.PhoneVerification.VerifyCode;

/// <summary>
/// Handler for VerifyCode command - wrapper for backward compatibility
/// Delegates to VerifyPhoneCommand and adds user lookup
/// </summary>
public sealed class VerifyCodeCommandHandler : ICommandHandler<VerifyCodeCommand, VerifyCodeResponse>
{
    private readonly ISender _mediator;
    private readonly IPhoneVerificationRepository _verificationRepo;
    private readonly ILogger<VerifyCodeCommandHandler> _logger;

    public VerifyCodeCommandHandler(
        ISender mediator,
        IPhoneVerificationRepository verificationRepo,
        ILogger<VerifyCodeCommandHandler> logger)
    {
        _mediator = mediator;
        _verificationRepo = verificationRepo;
        _logger = logger;
    }

    public async Task<VerifyCodeResponse> Handle(
        VerifyCodeCommand request,
        CancellationToken cancellationToken)
    {
        // Find verification by phone number
        var phoneNumber = PhoneNumber.From(request.PhoneNumber);
        var verification = await _verificationRepo.GetByPhoneNumberAsync(phoneNumber, cancellationToken);

        if (verification == null)
        {
            return new VerifyCodeResponse(
                Success: false,
                Message: "No verification found for this phone number",
                ErrorMessage: "Verification not found",
                RemainingAttempts: 0
            );
        }

        // Delegate to the new command
        var newCommand = new VerifyPhoneCommand(
            VerificationId: verification.Id.Value,
            Code: request.Code);

        var result = await _mediator.Send(newCommand, cancellationToken);

        if (!result.Success)
        {
            return new VerifyCodeResponse(
                Success: false,
                Message: result.Message,
                ErrorMessage: result.Message,
                RemainingAttempts: result.RemainingAttempts
            );
        }

        // TODO: User lookup and token generation should be handled here
        // For now, return success without user/token info
        // The controller should handle user creation/login separately
        _logger.LogWarning(
            "Phone verified successfully but user lookup not implemented. " +
            "Controller should handle user authentication separately. Phone: {Phone}",
            phoneNumber.Value);

        return new VerifyCodeResponse(
            Success: true,
            Message: result.Message,
            User: null, // TODO: Look up or create user
            AccessToken: null, // TODO: Generate tokens
            RefreshToken: null,
            ExpiresAt: result.VerifiedAt
        );
    }
}
