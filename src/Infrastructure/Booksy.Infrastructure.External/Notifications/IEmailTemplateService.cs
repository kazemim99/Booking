// ========================================
// Event Handlers - Domain Events
// ========================================

// Booksy.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
// Booksy.UserManagement.Application/Services/Interfaces/IEmailTemplateService.cs
namespace Booksy.Infrastructure.External.Notifications
{
    public interface IEmailTemplateService
    {
        Task SendEmailAsync(string to, string templateId, Dictionary<string, string> data, CancellationToken cancellationToken = default);
        Task SendBulkEmailAsync(List<string> recipients, string templateId, Dictionary<string, string> data, CancellationToken cancellationToken = default);
        Task<bool> IsEmailTemplateAvailableAsync(string templateId, CancellationToken cancellationToken = default);
    }
}

