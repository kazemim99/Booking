// ========================================
// Event Handlers - Domain Events
// ========================================

// Booksy.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
// Booksy.UserManagement.Application/Services/Interfaces/IEmailTemplateService.cs

namespace Booksy.Infrastructure.External.Notifications
{
    public interface IEmailService
    {
        Task SendBulkEmailAsync(List<string> recipients, string subject, string htmlBody, CancellationToken cancellationToken = default);
        Task SendEmailAsync(string to, string subject, string htmlBody, string? plainTextBody = null, CancellationToken cancellationToken = default);
    }
}

