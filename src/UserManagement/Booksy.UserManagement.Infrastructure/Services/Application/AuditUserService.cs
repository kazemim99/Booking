// ========================================
// Security Services
// ========================================

// Booksy.UserManagement.Infrastructure/Services/Security/PasswordHasher.cs
using Booksy.UserManagement.Application.Services.Interfaces;
using Booksy.Core.Application.Services;
using Booksy.UserManagement.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Infrastructure.Services.Application
{
    public class AuditUserService : IAuditUserService
    {
        private readonly ILogger<AuditUserService> _logger;
        private readonly List<AuditEntry> _auditLog = new(); // In production, this would be persisted

        public AuditUserService(ILogger<AuditUserService> logger)
        {
            _logger = logger;
        }

        public Task LogRegistrationAsync(UserId userId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
        {
            var entry = AuditEntry.Create(
                "User",
                userId.Value.ToString(),
                AuditActions.Create,
                userId.Value.ToString())
                .WithRequestInfo(ipAddress, userAgent);

            _auditLog.Add(entry);
            _logger.LogInformation("User registration audit: {UserId}", userId.Value);

            return Task.CompletedTask;
        }

        public Task LogActivationAsync(UserId userId, CancellationToken cancellationToken = default)
        {
            var entry = AuditEntry.Create(
                "User",
                userId.Value.ToString(),
                "ACTIVATION",
                userId.Value.ToString());

            _auditLog.Add(entry);
            _logger.LogInformation("User activation audit: {UserId}", userId.Value);

            return Task.CompletedTask;
        }

        public Task LogSuccessfulLoginAsync(UserId userId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
        {
            var entry = AuditEntry.Create(
                "User",
                userId.Value.ToString(),
                AuditActions.Login,
                userId.Value.ToString())
                .WithRequestInfo(ipAddress, userAgent);

            _auditLog.Add(entry);
            _logger.LogInformation("Successful login audit: {UserId}", userId.Value);

            return Task.CompletedTask;
        }

        public Task LogFailedLoginAsync(string email, string reason, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
        {
            var entry = AuditEntry.Create(
                "User",
                email,
                "FAILED_LOGIN",
                null,
                null,
                reason)
                .WithRequestInfo(ipAddress, userAgent);

            _auditLog.Add(entry);
            _logger.LogWarning("Failed login audit: {Email}, Reason: {Reason}", email, reason);

            return Task.CompletedTask;
        }

        public Task LogPasswordChangeAsync(UserId userId, CancellationToken cancellationToken = default)
        {
            var entry = AuditEntry.Create(
                "User",
                userId.Value.ToString(),
                AuditActions.PasswordChange,
                userId.Value.ToString());

            _auditLog.Add(entry);
            _logger.LogInformation("Password change audit: {UserId}", userId.Value);

            return Task.CompletedTask;
        }

        public Task LogPasswordResetRequestAsync(UserId userId, string? ipAddress, CancellationToken cancellationToken = default)
        {
            var entry = AuditEntry.Create(
                "User",
                userId.Value.ToString(),
                AuditActions.PasswordReset,
                userId.Value.ToString())
                .WithRequestInfo(ipAddress, null);

            _auditLog.Add(entry);
            _logger.LogInformation("Password reset request audit: {UserId}", userId.Value);

            return Task.CompletedTask;
        }

        public Task LogProfileUpdateAsync(UserId userId, Dictionary<string, object> changes, CancellationToken cancellationToken = default)
        {
            var entry = AuditEntry.Create(
                "UserProfile",
                userId.Value.ToString(),
                AuditActions.Update,
                userId.Value.ToString());

            foreach (var change in changes)
            {
                entry.AddMetadata(change.Key, change.Value);
            }

            _auditLog.Add(entry);
            _logger.LogInformation("Profile update audit: {UserId}", userId.Value);

            return Task.CompletedTask;
        }

        public Task LogAccountDeactivationAsync(UserId userId, string reason, CancellationToken cancellationToken = default)
        {
            var entry = AuditEntry.Create(
                "User",
                userId.Value.ToString(),
                "DEACTIVATION",
                userId.Value.ToString())
                .AddMetadata("reason", reason);

            _auditLog.Add(entry);
            _logger.LogInformation("Account deactivation audit: {UserId}, Reason: {Reason}", userId.Value, reason);

            return Task.CompletedTask;
        }
    }
}

