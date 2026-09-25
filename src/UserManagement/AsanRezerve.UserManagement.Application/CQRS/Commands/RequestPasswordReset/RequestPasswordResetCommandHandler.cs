// ========================================
// AsanRezerve.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Application.Abstractions.Persistence;
using AsanRezerve.Infrastructure.External.Notifications;
using AsanRezerve.UserManagement.Domain.Aggregates;
using MediatR;

namespace AsanRezerve.UserManagement.Application.CQRS.Commands.RequestPasswordReset
{
    public sealed class RequestPasswordResetCommandHandler : ICommandHandler<RequestPasswordResetCommand>
    {
        private readonly IUserRepository _userRepository;
        
        private readonly IUserManagementUnitOfWork _unitOfWork;
        private readonly IEmailTemplateService _emailService;
        private readonly IAuditUserService _auditService;
        private readonly ILogger<RequestPasswordResetCommandHandler> _logger;

        public RequestPasswordResetCommandHandler(
            IUserRepository userWriteRepository,
            
            IUserManagementUnitOfWork unitOfWork,
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
                // Don't reveal if user exists: same silent success as for a known address.
                // (This used to fall through and dereference null → a 500 that revealed it anyway.)
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
                return;
            }

            user.RequestPasswordReset();

            await _userRepository.UpdateAsync(user, cancellationToken);
            // Commit the UserManagement unit of work explicitly. The pipeline's TransactionBehavior
            // commits the ServiceCatalog context (registered last, DI last-wins), so without this the
            // change was tracked and then silently discarded at the end of the request.
            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

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
                ["ResetUrl"] = $"https://asanrezerve.com/reset-password?token={user.PasswordResetToken.Token}",
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

