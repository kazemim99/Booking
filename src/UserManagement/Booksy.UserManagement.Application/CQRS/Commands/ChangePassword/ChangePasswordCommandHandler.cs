// ========================================
// Booksy.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.External.Notifications;
using MediatR;

namespace Booksy.UserManagement.Application.CQRS.Commands.ChangePassword
{
    public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailTemplateService _emailService;
        private readonly IAuditUserService _auditService;
        private readonly ILogger<ChangePasswordCommandHandler> _logger;

        public ChangePasswordCommandHandler(
            IUserRepository userWriteRepository,
            IUnitOfWork unitOfWork,
            IEmailTemplateService emailService,
            IAuditUserService auditService,
            ILogger<ChangePasswordCommandHandler> logger)
        {
            _userRepository = userWriteRepository;
            _emailService = emailService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task Handle(
            ChangePasswordCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Changing password for user: {UserId}", request.UserId);

            var userId = UserId.From(request.UserId);
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            user.ChangePassword(request.CurrentPassword, request.NewPassword);

            if (request.LogoutAllSessions)
            {
                user.EndAllSessions();
                user.RevokeAllRefreshTokens();
            }

            await _userRepository.UpdateAsync(user, cancellationToken);

            // Send notification email
            await SendPasswordChangedEmailAsync(user.Email.Value, user.Profile.FirstName, cancellationToken);

            await _auditService.LogPasswordChangeAsync(user.Id, cancellationToken);

            _logger.LogInformation("Password changed successfully for user: {UserId}", request.UserId);

        }

    

        private async Task SendPasswordChangedEmailAsync(
            string email,
            string firstName,
            CancellationToken cancellationToken)
        {
            try
            {
                var emailData = new Dictionary<string, string>
                {
                    ["FirstName"] = firstName,
                    ["ChangedAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                };

                await _emailService.SendEmailAsync(
                    email,
                    "PASSWORD_CHANGED",
                    emailData,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send password changed email");
            }
        }
    }
}


