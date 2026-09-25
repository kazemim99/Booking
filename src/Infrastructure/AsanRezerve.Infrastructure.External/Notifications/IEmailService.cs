// ========================================
// Event Handlers - Domain Events
// ========================================

// AsanRezerve.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
// AsanRezerve.UserManagement.Application/Services/Interfaces/IEmailTemplateService.cs

namespace AsanRezerve.Infrastructure.External.Notifications
{
    public interface IEmailService
    {
        Task SendBulkEmailAsync(List<string> recipients, string subject, string htmlBody, CancellationToken cancellationToken = default);
        Task SendEmailAsync(string to, string subject, string htmlBody, string? plainTextBody = null, CancellationToken cancellationToken = default);
    }
}

