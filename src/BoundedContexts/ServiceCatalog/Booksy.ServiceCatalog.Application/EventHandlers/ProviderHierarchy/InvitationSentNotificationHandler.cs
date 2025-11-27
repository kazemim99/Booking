// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/ProviderHierarchy/InvitationSentNotificationHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.ServiceCatalog.Application.Services;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.ProviderHierarchy
{
    /// <summary>
    /// Sends SMS notification when an invitation is sent to a potential staff member
    /// </summary>
    public sealed class InvitationSentNotificationHandler : IDomainEventHandler<InvitationSentEvent>
    {
        private readonly ISmsNotificationService _smsService;
        private readonly IProviderReadRepository _providerRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InvitationSentNotificationHandler> _logger;

        public InvitationSentNotificationHandler(
            ISmsNotificationService smsService,
            IProviderReadRepository providerRepository,
            IConfiguration configuration,
            ILogger<InvitationSentNotificationHandler> logger)
        {
            _smsService = smsService;
            _providerRepository = providerRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task HandleAsync(InvitationSentEvent domainEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Sending invitation SMS for invitation {InvitationId} from organization {OrganizationId} to {PhoneNumber}",
                    domainEvent.InvitationId,
                    domainEvent.OrganizationId.Value,
                    domainEvent.PhoneNumber);

                // Get organization details
                var organization = await _providerRepository.GetByIdAsync(domainEvent.OrganizationId, cancellationToken);

                if (organization == null)
                {
                    _logger.LogWarning(
                        "Organization {OrganizationId} not found for invitation {InvitationId}",
                        domainEvent.OrganizationId.Value,
                        domainEvent.InvitationId);
                    return;
                }

                // Generate invitation link
                var baseUrl = _configuration["App:BaseUrl"] ?? "https://booksy.ir";
                var invitationLink = $"{baseUrl}/provider/invitations/{domainEvent.InvitationId}/accept?org={domainEvent.OrganizationId.Value}";

                _logger.LogInformation(
                    "Generated invitation link with InvitationId: {InvitationId}, Link: {Link}",
                    domainEvent.InvitationId,
                    invitationLink);

                // Construct SMS message (in Persian)
                var organizationName = organization.Profile?.BusinessName ?? "سازمان";
                var message = $"{organizationName} شما را به عنوان کارمند دعوت کرده است. برای پذیرش دعوت روی لینک کلیک کنید:\n{invitationLink}\n(اعتبار: 7 روز)";

                _logger.LogInformation(
                    "Sending SMS to {PhoneNumber} with invitation link for {InvitationId}",
                    domainEvent.PhoneNumber,
                    domainEvent.InvitationId);

                // Send SMS
                await _smsService.SendSmsAsync(
                    domainEvent.PhoneNumber,
                    message,
                    cancellationToken);

                _logger.LogInformation(
                    "Invitation SMS sent successfully for invitation {InvitationId}",
                    domainEvent.InvitationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending invitation SMS for invitation {InvitationId}",
                    domainEvent.InvitationId);

                // Don't throw - we don't want to fail the invitation creation if SMS fails
                // The invitation is still created and can be resent later
            }
        }
    }
}
