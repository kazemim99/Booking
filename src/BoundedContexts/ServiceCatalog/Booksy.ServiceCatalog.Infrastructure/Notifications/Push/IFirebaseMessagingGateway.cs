using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Notifications.Push
{
    /// <summary>What the gateway said about one send.</summary>
    /// <param name="Success">The gateway accepted the message.</param>
    /// <param name="MessageId">Its id, for support lookups.</param>
    /// <param name="ErrorMessage">Why it did not.</param>
    /// <param name="TokenIsDead">
    /// The token will never work again — the app was uninstalled, or the token was rotated. Distinct from an
    /// ordinary failure, which is worth retrying.
    /// </param>
    public sealed record PushSendResult(
        bool Success,
        string? MessageId,
        string? ErrorMessage,
        bool TokenIsDead = false);

    /// <summary>
    /// The boundary with Firebase.
    /// </summary>
    /// <remarks>
    /// Exists so the fan-out, retirement and skip rules above it can be tested without a Firebase project or
    /// a network. The previous implementation had no such seam, which is part of why a stub that never sent
    /// anything could sit in the codebase looking like a working service.
    /// </remarks>
    public interface IFirebaseMessagingGateway
    {
        /// <summary>False when this environment has no credentials. Sends are then skipped, never faked.</summary>
        bool IsConfigured { get; }

        Task<PushSendResult> SendAsync(
            string deviceToken,
            string title,
            string body,
            IReadOnlyDictionary<string, string> data,
            CancellationToken cancellationToken = default);

        Task<PushSendResult> SendToTopicAsync(
            string topic,
            string title,
            string body,
            IReadOnlyDictionary<string, string> data,
            CancellationToken cancellationToken = default);
    }

    /// <inheritdoc />
    public sealed class FirebaseMessagingGateway : IFirebaseMessagingGateway
    {
        /// <summary>
        /// Errors that mean the token is permanently gone, as opposed to a transient gateway problem.
        /// </summary>
        private static readonly HashSet<string> DeadTokenCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "UNREGISTERED",
            "INVALID_ARGUMENT",
            "SENDER_ID_MISMATCH",
        };

        private readonly ILogger<FirebaseMessagingGateway> _logger;
        private readonly FirebaseMessaging? _messaging;

        public FirebaseMessagingGateway(IConfiguration configuration, ILogger<FirebaseMessagingGateway> logger)
        {
            _logger = logger;

            var credentialsPath = configuration["Notifications:Firebase:CredentialsPath"];
            var credentialsJson = configuration["Notifications:Firebase:CredentialsJson"];

            try
            {
                var credential = ResolveCredential(credentialsPath, credentialsJson);
                if (credential is null)
                {
                    _logger.LogWarning(
                        "Firebase is not configured; push notifications will be skipped on this environment");
                    return;
                }

                // FirebaseApp is process-wide and throws if created twice, which matters here because tests
                // and multiple hosts share a process.
                var app = FirebaseApp.DefaultInstance
                          ?? FirebaseApp.Create(new AppOptions { Credential = credential });

                _messaging = FirebaseMessaging.GetMessaging(app);
                _logger.LogInformation("Firebase messaging initialised");
            }
            catch (Exception ex)
            {
                // A misconfigured environment must degrade to "push is skipped", never to a host that will
                // not start and never to a fabricated success.
                _logger.LogError(ex, "Firebase could not be initialised; push notifications will be skipped");
            }
        }

        public bool IsConfigured => _messaging is not null;

        public async Task<PushSendResult> SendAsync(
            string deviceToken,
            string title,
            string body,
            IReadOnlyDictionary<string, string> data,
            CancellationToken cancellationToken = default)
        {
            if (_messaging is null)
                return new PushSendResult(false, null, "Firebase is not configured");

            var message = new Message
            {
                Token = deviceToken,
                Notification = new Notification { Title = title, Body = body },
                Data = data.ToDictionary(kv => kv.Key, kv => kv.Value),
            };

            return await SendCoreAsync(message, cancellationToken);
        }

        public async Task<PushSendResult> SendToTopicAsync(
            string topic,
            string title,
            string body,
            IReadOnlyDictionary<string, string> data,
            CancellationToken cancellationToken = default)
        {
            if (_messaging is null)
                return new PushSendResult(false, null, "Firebase is not configured");

            var message = new Message
            {
                Topic = topic,
                Notification = new Notification { Title = title, Body = body },
                Data = data.ToDictionary(kv => kv.Key, kv => kv.Value),
            };

            return await SendCoreAsync(message, cancellationToken);
        }

        private async Task<PushSendResult> SendCoreAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                var messageId = await _messaging!.SendAsync(message, cancellationToken);
                return new PushSendResult(true, messageId, null);
            }
            catch (FirebaseMessagingException ex)
            {
                var dead = ex.MessagingErrorCode is MessagingErrorCode.Unregistered
                           || ex.MessagingErrorCode is MessagingErrorCode.SenderIdMismatch
                           || DeadTokenCodes.Contains(ex.ErrorCode.ToString());

                _logger.LogWarning(
                    ex,
                    "Firebase rejected a push ({ErrorCode}); token dead: {Dead}",
                    ex.MessagingErrorCode, dead);

                return new PushSendResult(false, null, ex.Message, dead);
            }
            catch (Exception ex)
            {
                // Transient: network, timeout. Reported as a failure so the retry budget applies.
                _logger.LogWarning(ex, "Push send failed");
                return new PushSendResult(false, null, ex.Message);
            }
        }

        private static GoogleCredential? ResolveCredential(string? path, string? json)
        {
            if (!string.IsNullOrWhiteSpace(json))
                return GoogleCredential.FromJson(json);

            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                return GoogleCredential.FromFile(path);

            return null;
        }
    }
}
