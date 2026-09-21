import 'dart:async';

import 'package:flutter/foundation.dart';

import 'push_token_source.dart';

/// Keeps this device registered with the backend for push, for exactly as long as someone is signed in on it.
///
/// Every public method swallows its own failures. Push is a convenience: a declined permission, a build
/// without Firebase configuration, or a request that fails must never be the reason signing in or out fails.
/// A failed registration is retried on the next sign-in or app start — both call [onSignedIn], and the backend
/// treats re-registering a known token as a refresh, not a duplicate.
/// The two moments the auth flow tells push about. An interface so the auth bloc can be tested without push,
/// and so a build that never wires push gets a harmless default.
abstract class PushLifecycle {
  Future<void> onSignedIn();
  Future<void> onSigningOut();
}

/// Does nothing — the default when no push registration is provided.
class NoPush implements PushLifecycle {
  const NoPush();

  @override
  Future<void> onSignedIn() async {}

  @override
  Future<void> onSigningOut() async {}
}

class PushRegistration implements PushLifecycle {
  final PushTokenSource _source;
  final DeviceTokenApi _api;

  StreamSubscription<String>? _refreshes;

  /// The token this device last registered. Revoking on sign-out must use THIS one: after a refresh, the
  /// first token is already dead and revoking it would leave the live one attached to the account.
  String? _registered;

  PushRegistration(this._source, this._api);

  @override
  Future<void> onSignedIn() async {
    try {
      if (!await _source.isAvailable()) return;
      if (!await _source.requestPermission()) return;

      final token = await _source.getToken();
      if (token == null || token.isEmpty) return;

      await _register(token);

      // Once per session, however many times sign-in is resolved (OTP, app restart, status refresh).
      _refreshes ??= _source.onTokenRefresh.listen((next) => _register(next));
    } catch (e) {
      debugPrint('[Push] registration skipped: $e');
    }
  }

  /// Must run BEFORE the session is cleared: revoking is an authenticated call, and after logout it would be
  /// answered 401 and the token would stay attached to the account on a handset that may change hands.
  @override
  Future<void> onSigningOut() async {
    await _refreshes?.cancel();
    _refreshes = null;

    final token = _registered;
    _registered = null;

    if (token != null) {
      try {
        await _api.revoke(token);
      } catch (e) {
        debugPrint('[Push] revoke failed; the server will retire the token when FCM rejects it: $e');
      }
    }

    try {
      await _source.deleteToken();
    } catch (e) {
      debugPrint('[Push] local token not deleted: $e');
    }
  }

  Future<void> _register(String token) async {
    try {
      await _api.register(token, _source.platform);
      _registered = token;
    } catch (e) {
      debugPrint('[Push] register failed, will retry on next sign-in: $e');
    }
  }
}
