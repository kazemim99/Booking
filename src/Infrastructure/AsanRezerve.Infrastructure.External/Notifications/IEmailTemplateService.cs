// ========================================
// Event Handlers - Domain Events
// ========================================

// AsanRezerve.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
// AsanRezerve.UserManagement.Application/Services/Interfaces/IEmailTemplateService.cs
namespace AsanRezerve.Infrastructure.External.Notifications
{
    public interface IEmailTemplateService
    {
        Task SendEmailAsync(string to, string templateId, Dictionary<string, string> data, CancellationToken cancellationToken = default);
        Task SendBulkEmailAsync(List<string> recipients, string templateId, Dictionary<string, string> data, CancellationToken cancellationToken = default);
        Task<bool> IsEmailTemplateAvailableAsync(string templateId, CancellationToken cancellationToken = default);
    }
}

