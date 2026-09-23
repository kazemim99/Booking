import 'package:firebase_core/firebase_core.dart';

/// Browser push configuration, fixed at build time.
///
/// CI passes the Firebase Web app's config and the VAPID public key as `--dart-define` values
/// (`.github/workflows/deploy.yml`, docs/DEPLOYMENT_RUNBOOK.md › Web push). All of them are public identifiers,
/// not secrets: they ship inside every web bundle. A build without them — every local build, and CI until they are
/// set — is not configured, and push on the web stays off exactly as it was before this existed.
class WebPushConfig {
  final String apiKey;
  final String appId;
  final String messagingSenderId;
  final String projectId;

  /// The public half of the Web Push key pair (Firebase console › Cloud Messaging › Web Push certificates).
  final String vapidKey;

  const WebPushConfig({
    required this.apiKey,
    required this.appId,
    required this.messagingSenderId,
    required this.projectId,
    required this.vapidKey,
  });

  /// The values this build was compiled with. `String.fromEnvironment` only works reliably in a const context.
  static const fromEnvironment = WebPushConfig(
    apiKey: String.fromEnvironment('FIREBASE_WEB_API_KEY'),
    appId: String.fromEnvironment('FIREBASE_WEB_APP_ID'),
    messagingSenderId: String.fromEnvironment('FIREBASE_WEB_MESSAGING_SENDER_ID'),
    projectId: String.fromEnvironment('FIREBASE_WEB_PROJECT_ID'),
    vapidKey: String.fromEnvironment('FIREBASE_WEB_VAPID_KEY'),
  );

  /// Every value present. Half a config would fail in the browser rather than at build time, so it counts as none.
  bool get isComplete => [apiKey, appId, messagingSenderId, projectId, vapidKey].every((v) => v.trim().isNotEmpty);

  FirebaseOptions toFirebaseOptions() => FirebaseOptions(
        apiKey: apiKey,
        appId: appId,
        messagingSenderId: messagingSenderId,
        projectId: projectId,
      );

  /// Where the push service worker is registered from (relative to the page, so it follows `<base href>`).
  ///
  /// Under `push/`, so its scope is `/push/`: Flutter's loader re-registers its own worker at `/` whenever a
  /// registration covers the page, and a push worker there would be replaced on the next load. The config rides in
  /// the query because a service worker cannot read dart-defines; web/push/firebase-messaging-sw.js reads it back.
  /// The VAPID key is not needed there — the page subscribes.
  String get serviceWorkerPath => Uri(
        path: 'push/firebase-messaging-sw.js',
        queryParameters: {
          'apiKey': apiKey,
          'appId': appId,
          'messagingSenderId': messagingSenderId,
          'projectId': projectId,
        },
      ).toString();

  WebPushConfig copyWith({
    String? apiKey,
    String? appId,
    String? messagingSenderId,
    String? projectId,
    String? vapidKey,
  }) =>
      WebPushConfig(
        apiKey: apiKey ?? this.apiKey,
        appId: appId ?? this.appId,
        messagingSenderId: messagingSenderId ?? this.messagingSenderId,
        projectId: projectId ?? this.projectId,
        vapidKey: vapidKey ?? this.vapidKey,
      );
}
