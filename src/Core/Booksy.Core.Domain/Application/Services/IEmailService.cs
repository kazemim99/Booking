namespace Booksy.Core.Domain.Application.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(
            string to,
            string subject,
            string htmlBody,
            string? textBody = null,
            CancellationToken cancellationToken = default);
    }

}