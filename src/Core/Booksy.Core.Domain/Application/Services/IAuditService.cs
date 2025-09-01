namespace Booksy.Core.Domain.Application.Services
{
    public interface IAuditService
    {
        Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    }
}