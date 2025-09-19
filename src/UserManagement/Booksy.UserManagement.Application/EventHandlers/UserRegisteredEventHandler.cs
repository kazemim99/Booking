// ========================================
// Event Handlers - Domain Events
// ========================================

// Booksy.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
using Booksy.Core.Application.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.UserManagement.Domain.Events;

namespace Booksy.UserManagement.Application.EventHandlers
{
    public sealed class UserRegisteredEventHandler : IDomainEventHandler<UserRegisteredEvent>
    {
        private readonly IEmailTemplateService _emailService;
        private readonly ILogger<UserRegisteredEventHandler> _logger;

        public UserRegisteredEventHandler(
            IEmailTemplateService emailService,
            ILogger<UserRegisteredEventHandler> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task HandleAsync(UserRegisteredEvent domainEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Handling UserRegisteredEvent for UserId: {UserId}",
                domainEvent.UserId);

            try
            {
                // Send verification email if needed
                if (domainEvent.UserType == Domain.Enums.UserType.Customer)
                {
                    var emailData = new Dictionary<string, string>
                    {
                        ["FirstName"] = domainEvent.FirstName,
                        ["Email"] = domainEvent.Email.Value
                    };

                    await _emailService.SendEmailAsync(
                        domainEvent.Email.Value,
                        "WELCOME",
                        emailData,
                        cancellationToken);
                }

                // Additional post-registration processing could go here
                // - Analytics tracking
                // - CRM integration
                // - Welcome workflow initiation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling UserRegisteredEvent for UserId: {UserId}", domainEvent.UserId);
                // Don't throw - event handlers should be resilient
            }
        }
    }
}


