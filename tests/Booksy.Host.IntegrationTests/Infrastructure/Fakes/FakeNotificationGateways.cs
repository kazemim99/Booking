using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.Tests.Commons;
using CoreNotifications = Booksy.Core.Application.Services.Notifications;

namespace Booksy.Host.IntegrationTests.Infrastructure.Fakes;

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
public sealed class FakeEmailNotificationService : IEmailNotificationService, IResettableFake
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _lastSubjectByRecipient = new();

    /// <summary>The subject of the most recent e-mail sent to this address, or null if none.</summary>
    public string? LastSubjectTo(string email) =>
        _lastSubjectByRecipient.TryGetValue(email, out var subject) ? subject : null;

    public void Reset() => _lastSubjectByRecipient.Clear();

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

/// <summary>
/// Always-succeeds, no-network SMS channel, shared by ServiceCatalog and UserManagement tests alike
/// (docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 4 — one shared factory means one
/// <see cref="CoreNotifications.ISmsNotificationService"/> registration for both, since only the
/// last one registered wins regardless of which bounded context's <c>AddXInfrastructure</c> ran
/// first). Captures every sent message so a test can recover an OTP code from it — the
/// <c>PhoneVerification</c> aggregate itself never persists the plaintext code, only its hash.
/// Replaces ServiceCatalog's older, non-capturing <c>FakeSmsGateway</c>: the merge kept whichever
/// of the two fakes could do more, not either arbitrarily.
/// </summary>
public sealed class FakeSmsNotificationService : CoreNotifications.ISmsNotificationService, IResettableFake
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _lastMessageByPhone = new();

    /// <summary>The most recent message sent to this phone number (E.164 or as passed), or null if none.</summary>
    public string? LastMessageTo(string phoneNumber) =>
        _lastMessageByPhone.TryGetValue(phoneNumber, out var message) ? message : null;

    public void Reset() => _lastMessageByPhone.Clear();

    public Task<(bool Success, string? MessageId, string? ErrorMessage)> SendSmsAsync(
        string phoneNumber,
        string message,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        _lastMessageByPhone[phoneNumber] = message;
        return Task.FromResult<(bool, string?, string?)>((true, $"fake-{Guid.NewGuid():N}", null));
    }

    public Task<List<(string PhoneNumber, bool Success, string? MessageId, string? ErrorMessage)>> SendBulkSmsAsync(
        List<string> phoneNumbers,
        string message,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var phone in phoneNumbers)
        {
            _lastMessageByPhone[phone] = message;
        }

        return Task.FromResult(phoneNumbers
            .Select(p => (p, true, (string?)$"fake-{Guid.NewGuid():N}", (string?)null))
            .ToList());
    }
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
