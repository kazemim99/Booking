import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';

import 'push_token_source.dart';
import 'web_push_config.dart';

/// The only class in the app that touches Firebase.
///
/// Initialisation is attempted once, lazily, and a failure means "push is not available on this build" — not
/// a crash. A bare `Firebase.initializeApp()` throws when the platform configuration file
/// (`google-services.json` / `GoogleService-Info.plist`) is absent, and that file is account-specific and not
/// in the repository; without this guard every build lacking it would die at startup.
///
/// **Web** (the production app is Flutter web on Android Chrome): available only when the build was given the
/// Firebase Web app's config and VAPID key ([WebPushConfig]); without them it is off, exactly as before web push
/// existed. The Firebase JS SDK is fetched from www.gstatic.com and an unreachable host would hang initialisation
/// forever, so it is bounded — push is then off for the session and the app is unaffected.
class FirebasePushTokenSource implements PushTokenSource {
  final WebPushConfig _web;
  final bool _isWeb;

  bool? _available;

  /// A browser can only delete a token through the service worker registration that `getToken` sets up in this
  /// page's session; deleting without one makes Firebase try to register a default worker this app does not serve.
  bool _webTokenFetched = false;

  static const _initTimeout = Duration(seconds: 20);

  FirebasePushTokenSource({WebPushConfig web = WebPushConfig.fromEnvironment, bool isWeb = kIsWeb})
      : _web = web,
        _isWeb = isWeb;

  @override
  Future<bool> isAvailable() async {
    if (_available != null) return _available!;
    if (_isWeb && !_web.isComplete) return _available = false;

    try {
      if (Firebase.apps.isEmpty) {
        await (_isWeb ? Firebase.initializeApp(options: _web.toFirebaseOptions()) : Firebase.initializeApp())
            .timeout(_initTimeout);
      }
      // Browsers without the Push API (Safari outside an installed web app, some in-app browsers).
      if (_isWeb && !await FirebaseMessaging.instance.isSupported()) return _available = false;
      return _available = true;
    } catch (e) {
      debugPrint('[Push] Firebase not available on this build; push disabled: $e');
      return _available = false;
    }
  }

  @override
  bool get promptNeedsUserAction => _isWeb;

  @override
  Future<PushPermission> permissionStatus() async {
    final settings = await FirebaseMessaging.instance.getNotificationSettings();
    return switch (settings.authorizationStatus) {
      AuthorizationStatus.authorized || AuthorizationStatus.provisional => PushPermission.granted,
      AuthorizationStatus.denied || AuthorizationStatus.deniedPermanently => PushPermission.denied,
      AuthorizationStatus.notDetermined => PushPermission.notDetermined,
    };
  }

  @override
  Future<bool> requestPermission() async {
    final settings = await FirebaseMessaging.instance.requestPermission();
    return settings.authorizationStatus == AuthorizationStatus.authorized ||
        settings.authorizationStatus == AuthorizationStatus.provisional;
  }

  @override
  Future<String?> getToken() async {
    if (!_isWeb) return FirebaseMessaging.instance.getToken();

    final token = await FirebaseMessaging.instance.getToken(
      vapidKey: _web.vapidKey,
      serviceWorkerScriptPath: _web.serviceWorkerPath,
    );
    _webTokenFetched = true;
    return token;
  }

  @override
  Stream<String> get onTokenRefresh => FirebaseMessaging.instance.onTokenRefresh;

  @override
  Future<void> deleteToken() async {
    if (!await isAvailable()) return;
    if (_isWeb && !_webTokenFetched) return;
    await FirebaseMessaging.instance.deleteToken();
    _webTokenFetched = false;
  }

  @override
  String get platform {
    if (_isWeb) return 'Web';
    return defaultTargetPlatform == TargetPlatform.iOS ? 'Ios' : 'Android';
  }
}
