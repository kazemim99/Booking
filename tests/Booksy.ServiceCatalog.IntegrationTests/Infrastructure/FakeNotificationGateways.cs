using Booksy.ServiceCatalog.Application.Services.Notifications;
using CoreNotifications = Booksy.Core.Application.Services.Notifications;

namespace Booksy.ServiceCatalog.IntegrationTests.Infrastructure;

/// <summary>
/// Deterministic, no-network notification channels for the integration tests.
///
/// The real implementations talk to SMTP, an SMS gateway, Firebase and SignalR. Inside the test
/// sandbox every one of them fails, and because <see cref="NotificationDispatcher"/> correctly
/// treats a dead gateway as a failed delivery attempt (rather than throwing), every notification
/// the API sends comes back <c>success: false</c> — so the tests measured the sandbox's lack of
/// outbound network, not the application's behaviour. These fakes always succeed and record what
/// they were asked to send, which is what lets a test assert the notification lifecycle
/// (queue → send → delivered, history, analytics, retry) through the real pipeline.
///
/// Gateway-specific behaviour (SMTP failures, FCM tokens, SignalR connection state) is not faked
/// here; it belongs to those adapters' own tests.
/// </summary>
public sealed class FakeEmailNotificationService : IEmailNotificationService
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _lastSubjectByRecipient = new();

    /// <summary>The subject of the most recent e-mail sent to this address, or null if none.</summary>
    public string? LastSubjectTo(string email) =>
        _lastSubjectByRecipient.TryGetValue(email, out var subject) ? subject : null;

    public Task<(bool Success, string? MessageId, string? ErrorMessage)> SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        string? fromName = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        _lastSubjectByRecipient[to] = subject;
        return Task.FromResult<(bool, string?, string?)>((true, $"fake-email-{Guid.NewGuid():N}", null));
    }

    public Task<List<(string Email, bool Success, string? MessageId, string? ErrorMessage)>> SendBulkEmailAsync(
        List<string> recipients,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var recipient in recipients)
        {
            _lastSubjectByRecipient[recipient] = subject;
        }

        return Task.FromResult(recipients
            .Select(r => (r, true, (string?)$"fake-email-{Guid.NewGuid():N}", (string?)null))
            .ToList());
    }
}

/// <summary>Always-succeeds SMS channel. See <see cref="FakeEmailNotificationService"/>.</summary>
public sealed class FakeSmsGateway : CoreNotifications.ISmsNotificationService
{
    public Task<(bool Success, string? MessageId, string? ErrorMessage)> SendSmsAsync(
        string phoneNumber,
        string message,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(bool, string?, string?)>((true, $"fake-sms-{Guid.NewGuid():N}", null));

    public Task<List<(string PhoneNumber, bool Success, string? MessageId, string? ErrorMessage)>> SendBulkSmsAsync(
        List<string> phoneNumbers,
        string message,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(phoneNumbers
            .Select(p => (p, true, (string?)$"fake-sms-{Guid.NewGuid():N}", (string?)null))
            .ToList());
}

/// <summary>Always-succeeds push channel. See <see cref="FakeEmailNotificationService"/>.</summary>
public sealed class FakePushNotificationService : IPushNotificationService
{
    public Task<(bool Success, string? MessageId, string? ErrorMessage)> SendPushAsync(
        string deviceToken,
        string title,
        string body,
        Dictionary<string, object>? data = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(bool, string?, string?)>((true, $"fake-push-{Guid.NewGuid():N}", null));

    public Task<(bool Success, string? MessageId, string? ErrorMessage)> SendPushAsync(
        Guid userId,
        string title,
        string body,
        Dictionary<string, object>? data = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(bool, string?, string?)>((true, $"fake-push-{Guid.NewGuid():N}", null));

    public Task<List<(string DeviceToken, bool Success, string? MessageId, string? ErrorMessage)>> SendBulkPushAsync(
        List<string> deviceTokens,
        string title,
        string body,
        Dictionary<string, object>? data = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(deviceTokens
            .Select(t => (t, true, (string?)$"fake-push-{Guid.NewGuid():N}", (string?)null))
            .ToList());

    public Task<(bool Success, string? MessageId, string? ErrorMessage)> SendToTopicAsync(
        string topic,
        string title,
        string body,
        Dictionary<string, object>? data = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(bool, string?, string?)>((true, $"fake-push-{Guid.NewGuid():N}", null));
}

/// <summary>Always-succeeds in-app channel. See <see cref="FakeEmailNotificationService"/>.</summary>
public sealed class FakeInAppNotificationService : IInAppNotificationService
{
    public Task<(bool Success, string? ErrorMessage)> SendToUserAsync(
        Guid userId,
        string title,
        string message,
        string type,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(bool, string?)>((true, null));

    public Task<(bool Success, string? ErrorMessage)> SendToUsersAsync(
        List<Guid> userIds,
        string title,
        string message,
        string type,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(bool, string?)>((true, null));

    public Task<(bool Success, string? ErrorMessage)> SendToAllAsync(
        string title,
        string message,
        string type,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(bool, string?)>((true, null));

    public Task<bool> IsUserConnectedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
