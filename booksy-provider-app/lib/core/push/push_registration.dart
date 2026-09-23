import 'dart:async';

import 'package:flutter/foundation.dart';

import 'push_token_source.dart';

/// The two moments the auth flow tells push about. An interface so the auth bloc can be tested without push,
/// and so a build that never wires push gets a harmless default.
abstract class PushLifecycle {
  Future<void> onSignedIn();
  Future<void> onSigningOut();
}

/// Where notifications stand on this device, as the notifications row and the one-time card show it.
enum PushStatus {
  /// This build or browser cannot receive push (no Firebase configuration, or a browser without push support).
  unavailable,

  /// Nobody has been asked yet.
  notAsked,

  /// Allowed. The device is registered while someone is signed in.
  enabled,

  /// Declined. Only the browser's or phone's own settings can undo it; asking again shows nothing.
  blocked,

  /// Allowed, but this device could not be connected: the messaging service (Google's, for FCM) or our server could
  /// not be reached. Nothing can arrive until it is; trying again does not ask again. Only [PushSettings.enable]
  /// reports it — after a restart the next sign-in retries silently.
  unreachable,
}

/// What the notifications screen can do: read the status, and turn notifications on from a tap.
abstract class PushSettings {
  /// Never prompts.
  Future<PushStatus> status();

  /// Asks for permission and, if someone is signed in, registers this device. Call it from a tap: in a browser the
  /// prompt is only shown in response to one. Never throws.
  Future<PushStatus> enable();
}

/// Does nothing — the default when no push registration is provided.
class NoPush implements PushLifecycle, PushSettings {
  const NoPush();

  @override
  Future<void> onSignedIn() async {}

  @override
  Future<void> onSigningOut() async {}

  @override
  Future<PushStatus> status() async => PushStatus.unavailable;

  @override
  Future<PushStatus> enable() async => PushStatus.unavailable;
}

/// Keeps this device registered with the backend for push, for exactly as long as someone is signed in on it.
///
/// Every public method swallows its own failures. Push is a convenience: a declined permission, a build
/// without Firebase configuration, or a request that fails must never be the reason signing in or out fails.
/// A failed registration is retried on the next sign-in or app start — both call [onSignedIn], and the backend
/// treats re-registering a known token as a refresh, not a duplicate.
///
/// **Browsers are asked only from a tap.** On Android the permission is requested at sign-in, as before. In a
/// browser, sign-in registers only a browser that was allowed earlier; the prompt belongs to [enable], which the
/// More row and the one-time card on Home call from the person's tap.
class PushRegistration implements PushLifecycle, PushSettings {
  final PushTokenSource _source;
  final DeviceTokenApi _api;

  StreamSubscription<String>? _refreshes;

  /// The token this device last registered. Revoking on sign-out must use THIS one: after a refresh, the
  /// first token is already dead and revoking it would leave the live one attached to the account.
  String? _registered;

  /// Registering is an authenticated call; turning notifications on while signed out waits for the next sign-in.
  bool _signedIn = false;

  PushRegistration(this._source, this._api);

  @override
  Future<void> onSignedIn() async {
    _signedIn = true;
    try {
      if (!await _source.isAvailable()) return;

      if (_source.promptNeedsUserAction) {
        // Signing in is not a tap on "notify me". Register only what the person already allowed.
        if (await _source.permissionStatus() != PushPermission.granted) return;
      } else if (!await _source.requestPermission()) {
        return;
      }

      await _registerCurrentToken();
    } catch (e) {
      debugPrint('[Push] registration skipped: $e');
    }
  }

  @override
  Future<PushStatus> status() async {
    try {
      if (!await _source.isAvailable()) return PushStatus.unavailable;
      return switch (await _source.permissionStatus()) {
        PushPermission.granted => PushStatus.enabled,
        PushPermission.denied => PushStatus.blocked,
        PushPermission.notDetermined => PushStatus.notAsked,
      };
    } catch (e) {
      debugPrint('[Push] status unknown: $e');
      return PushStatus.unavailable;
    }
  }

  @override
  Future<PushStatus> enable() async {
    try {
      if (!await _source.isAvailable()) return PushStatus.unavailable;

      // First thing after the tap: a browser only shows the prompt while the tap's activation is fresh.
      if (!await _source.requestPermission()) return await status();

      // Signed out: the next sign-in registers the allowed device.
      if (!_signedIn) return PushStatus.enabled;
      return await _registerCurrentToken() ? PushStatus.enabled : PushStatus.unreachable;
    } catch (e) {
      debugPrint('[Push] enabling failed: $e');
      return await status();
    }
  }

  /// Must run BEFORE the session is cleared: revoking is an authenticated call, and after logout it would be
  /// answered 401 and the token would stay attached to the account on a handset that may change hands.
  @override
  Future<void> onSigningOut() async {
    _signedIn = false;
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

  /// True when this device is now registered with the server.
  Future<bool> _registerCurrentToken() async {
    final String? token;
    try {
      token = await _source.getToken();
    } catch (e) {
      debugPrint('[Push] no token (messaging service unreachable?), will retry on next sign-in: $e');
      return false;
    }
    if (token == null || token.isEmpty) return false;

    final registered = await _register(token);

    // Once per session, however many times sign-in is resolved (OTP, app restart, status refresh).
    _refreshes ??= _source.onTokenRefresh.listen((next) => _register(next));
    return registered;
  }

  Future<bool> _register(String token) async {
    try {
      await _api.register(token, _source.platform);
      _registered = token;
      return true;
    } catch (e) {
      debugPrint('[Push] register failed, will retry on next sign-in: $e');
      return false;
    }
  }
}
