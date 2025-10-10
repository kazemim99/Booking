// ========================================
// Event Handlers - Domain Events
// ========================================

// Booksy.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
using Booksy.Core.Application.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.Infrastructure.External.Notifications;
using Booksy.UserManagement.Domain.Events;


namespace Booksy.UserManagement.Application.EventHandlers
{
    public sealed class UserActivatedEventHandler : IDomainEventHandler<UserActivatedEvent>
    {
        private readonly IEmailTemplateService _emailService;
        private readonly ILogger<UserActivatedEventHandler> _logger;

        public UserActivatedEventHandler(
            IEmailTemplateService emailService,
            ILogger<UserActivatedEventHandler> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task HandleAsync(UserActivatedEvent domainEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Handling UserActivatedEvent for UserId: {UserId}",
                domainEvent.UserId);

            try
            {
                // Send activation confirmation email
                var emailData = new Dictionary<string, string>
                {
                    ["Email"] = domainEvent.Email.Value,
                    ["ActivatedAt"] = domainEvent.ActivatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC")
                };

                await _emailService.SendEmailAsync(
                    domainEvent.Email.Value,
                    "ACTIVATION_CONFIRMED",
                    emailData,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling UserActivatedEvent for UserId: {UserId}", domainEvent.UserId);
            }
        }
    }
}
