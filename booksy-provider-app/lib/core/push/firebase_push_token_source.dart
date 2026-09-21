import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';

import 'push_token_source.dart';

/// The only class in the app that touches Firebase.
///
/// Initialisation is attempted once, lazily, and a failure means "push is not available on this build" — not
/// a crash. A bare `Firebase.initializeApp()` throws when the platform configuration file
/// (`google-services.json` / `GoogleService-Info.plist`) is absent, and that file is account-specific and not
/// in the repository; without this guard every build lacking it would die at startup.
///
/// Web is deliberately unsupported: browser push needs its own service worker and VAPID key, and nobody has
/// decided the salon app should prompt for notifications in a browser.
class FirebasePushTokenSource implements PushTokenSource {
  bool? _available;

  @override
  Future<bool> isAvailable() async {
    if (_available != null) return _available!;
    if (kIsWeb) return _available = false;

    try {
      if (Firebase.apps.isEmpty) await Firebase.initializeApp();
      return _available = true;
    } catch (e) {
      debugPrint('[Push] Firebase not configured on this build; push disabled: $e');
      return _available = false;
    }
  }

  @override
  Future<bool> requestPermission() async {
    final settings = await FirebaseMessaging.instance.requestPermission();
    return settings.authorizationStatus == AuthorizationStatus.authorized ||
        settings.authorizationStatus == AuthorizationStatus.provisional;
  }

  @override
  Future<String?> getToken() => FirebaseMessaging.instance.getToken();

  @override
  Stream<String> get onTokenRefresh => FirebaseMessaging.instance.onTokenRefresh;

  @override
  Future<void> deleteToken() async {
    if (await isAvailable()) await FirebaseMessaging.instance.deleteToken();
  }

  @override
  String get platform => defaultTargetPlatform == TargetPlatform.iOS ? 'Ios' : 'Android';
}
