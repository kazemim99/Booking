// ========================================
// Booksy.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.External.Notifications;
using Booksy.UserManagement.Domain.Aggregates;
using MediatR;

namespace Booksy.UserManagement.Application.CQRS.Commands.RequestPasswordReset
{
    public sealed class RequestPasswordResetCommandHandler : ICommandHandler<RequestPasswordResetCommand>
    {
        private readonly IUserRepository _userRepository;
        
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailTemplateService _emailService;
        private readonly IAuditUserService _auditService;
        private readonly ILogger<RequestPasswordResetCommandHandler> _logger;

        public RequestPasswordResetCommandHandler(
            IUserRepository userWriteRepository,
            
            IUnitOfWork unitOfWork,
            IEmailTemplateService emailService,
            IAuditUserService auditService,
            ILogger<RequestPasswordResetCommandHandler> logger)
        {
            _userRepository = userWriteRepository;
            
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task Handle(
            RequestPasswordResetCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing password reset request for email: {Email}", request.Email);

            var email = Email.Create(request.Email);
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

            if (user == null)
            {
                // Don't reveal if user exists
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
            }

            user.RequestPasswordReset();

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            // Send password reset email
            await SendPasswordResetEmailAsync(user, cancellationToken);

            await _auditService.LogPasswordResetRequestAsync(
                user.Id,
                request.IpAddress,
                cancellationToken);

            _logger.LogInformation("Password reset email sent to: {Email}", request.Email);

        }

        private async Task SendPasswordResetEmailAsync(
            User user,
            CancellationToken cancellationToken)
        {
            var emailData = new Dictionary<string, string>
            {
                ["FirstName"] = user.Profile.FirstName,
                ["ResetToken"] = user.PasswordResetToken!.Token,
                ["ResetUrl"] = $"https://booksy.com/reset-password?token={user.PasswordResetToken.Token}",
                ["ExpiresAt"] = user.PasswordResetToken.ExpiresAt.ToString("yyyy-MM-dd HH:mm:ss UTC")
            };

            await _emailService.SendEmailAsync(
                user.Email.Value,
                EmailTemplate.Templates.PasswordReset,
                emailData,
                cancellationToken);
        }
    }
}

