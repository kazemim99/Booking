import 'dart:async';

import 'package:booksy_customer_app/core/push/push_registration.dart';
import 'package:booksy_customer_app/core/push/push_token_source.dart';
import 'package:flutter_test/flutter_test.dart';

/// Registering this device so a push has somewhere to arrive.
///
/// Until this existed no client registered a token anywhere, so every push the backend sent resolved to "no
/// device" and was skipped. The rules held here: nothing about push may ever break signing in or out — a
/// declined permission, a build without Firebase, or a failed request are all quiet no-ops — and signing out
/// revokes the token THIS device registered, so a handset handed to someone else stops receiving this customer's
/// notices.
class _FakeSource implements PushTokenSource {
  bool available = true;

  /// What the person answers when asked.
  bool permission = true;

  /// What the platform reports without asking.
  PushPermission current = PushPermission.notDetermined;

  /// A browser: the prompt may only follow a tap.
  bool browser = false;

  int prompts = 0;
  String? token = 'token-1';

  /// The messaging service cannot be reached (Google's endpoints, from a network that blocks them).
  Object? tokenFails;
  int deleted = 0;
  final refreshes = StreamController<String>.broadcast();

  @override
  Future<bool> isAvailable() async => available;

  @override
  bool get promptNeedsUserAction => browser;

  @override
  Future<PushPermission> permissionStatus() async => current;

  @override
  Future<bool> requestPermission() async {
    prompts++;
    current = permission ? PushPermission.granted : PushPermission.denied;
    return permission;
  }

  @override
  Future<String?> getToken() async {
    if (tokenFails != null) throw tokenFails!;
    return token;
  }

  @override
  Stream<String> get onTokenRefresh => refreshes.stream;

  @override
  Future<void> deleteToken() async => deleted++;

  @override
  String get platform => browser ? 'Web' : 'Android';
}

class _FakeApi implements DeviceTokenApi {
  final registered = <String>[];
  final revoked = <String>[];
  Object? failWith;

  @override
  Future<void> register(String token, String platform) async {
    if (failWith != null) throw failWith!;
    registered.add('$token@$platform');
  }

  @override
  Future<void> revoke(String token) async {
    if (failWith != null) throw failWith!;
    revoked.add(token);
  }
}

void main() {
  late _FakeSource source;
  late _FakeApi api;
  late PushRegistration push;

  setUp(() {
    source = _FakeSource();
    api = _FakeApi();
    push = PushRegistration(source, api);
  });

  tearDown(() => source.refreshes.close());

  test('signing in registers this device', () async {
    await push.onSignedIn();

    expect(api.registered, ['token-1@Android']);
  });

  test('a declined permission registers nothing and fails nothing', () async {
    source.permission = false;

    await push.onSignedIn();

    expect(api.registered, isEmpty);
  });

  test('a build without Firebase registers nothing and fails nothing', () async {
    source.available = false;

    await push.onSignedIn();

    expect(api.registered, isEmpty);
  });

  test('a failed registration does not reach the sign-in flow', () async {
    api.failWith = Exception('offline');

    await expectLater(push.onSignedIn(), completes);
  });

  test('a refreshed token is registered in place of the old one', () async {
    await push.onSignedIn();

    source.refreshes.add('token-2');
    await Future<void>.delayed(Duration.zero);

    expect(api.registered, ['token-1@Android', 'token-2@Android']);
  });

  test('signing in twice does not listen for refreshes twice', () async {
    await push.onSignedIn();
    await push.onSignedIn();

    source.refreshes.add('token-2');
    await Future<void>.delayed(Duration.zero);

    expect(api.registered.where((r) => r.startsWith('token-2')), hasLength(1));
  });

  test('signing out revokes the token this device registered, then forgets it', () async {
    await push.onSignedIn();

    await push.onSigningOut();

    expect(api.revoked, ['token-1']);
    expect(source.deleted, 1);
  });

  test('signing out revokes the LATEST token after a refresh', () async {
    await push.onSignedIn();
    source.refreshes.add('token-2');
    await Future<void>.delayed(Duration.zero);

    await push.onSigningOut();

    expect(api.revoked, ['token-2']);
  });

  test('signing out on a device that never registered asks the server nothing', () async {
    source.permission = false;
    await push.onSignedIn();

    await push.onSigningOut();

    expect(api.revoked, isEmpty);
  });

  test('a failed revoke does not stop signing out', () async {
    await push.onSignedIn();
    api.failWith = Exception('offline');

    await expectLater(push.onSigningOut(), completes);
    expect(source.deleted, 1, reason: 'the local token is still forgotten');
  });

  test('after signing out, a refresh is no longer registered', () async {
    await push.onSignedIn();
    await push.onSigningOut();

    source.refreshes.add('token-3');
    await Future<void>.delayed(Duration.zero);

    expect(api.registered.where((r) => r.startsWith('token-3')), isEmpty);
  });

  /// In a browser the permission prompt may only follow a tap: Firefox refuses it otherwise and Chrome buries it
  /// behind a quiet icon. Signing in is not a tap on "notify me", so it never asks — the person asks, from the
  /// profile row or the one-time card, and that is what registers the browser.
  group('in a browser', () {
    setUp(() => source.browser = true);

    test('signing in never shows the permission prompt', () async {
      await push.onSignedIn();

      expect(source.prompts, 0);
      expect(api.registered, isEmpty);
    });

    test('signing in registers a browser that was allowed earlier, without asking again', () async {
      source.current = PushPermission.granted;

      await push.onSignedIn();

      expect(source.prompts, 0);
      expect(api.registered, ['token-1@Web']);
    });

    test('a browser that blocked notifications registers nothing', () async {
      source.current = PushPermission.denied;

      await push.onSignedIn();

      expect(api.registered, isEmpty);
    });

    test('turning notifications on while signed in asks, then registers at once', () async {
      await push.onSignedIn();

      final status = await push.enable();

      expect(source.prompts, 1);
      expect(status, PushStatus.enabled);
      expect(api.registered, ['token-1@Web']);
    });

    test('turning them on and then declining registers nothing and reports blocked', () async {
      source.permission = false;
      await push.onSignedIn();

      final status = await push.enable();

      expect(status, PushStatus.blocked);
      expect(api.registered, isEmpty);
    });

    test('turning them on while signed out asks, and registers once someone signs in', () async {
      final status = await push.enable();

      expect(status, PushStatus.enabled);
      expect(api.registered, isEmpty, reason: 'registering is an authenticated call');

      await push.onSignedIn();

      expect(api.registered, ['token-1@Web']);
    });

    test('after signing out, turning them on registers nothing for the previous account', () async {
      await push.onSignedIn();
      await push.onSigningOut();

      await push.enable();

      expect(api.registered, isEmpty);
    });

    test('a refresh after turning them on is registered too', () async {
      await push.onSignedIn();
      await push.enable();

      source.refreshes.add('token-2');
      await Future<void>.delayed(Duration.zero);

      expect(api.registered, ['token-1@Web', 'token-2@Web']);
    });

    test('a failure while turning them on is reported, never thrown at the button', () async {
      await push.onSignedIn();
      api.failWith = Exception('offline');

      await expectLater(push.enable(), completes);
    });

    // From Iran, Google's messaging endpoints are often unreachable without a VPN. Allowed-but-not-connected must not
    // be shown as "on": the person would wait for notifications that cannot arrive.
    test('allowed, but the messaging service cannot be reached: reported as unreachable, not as on', () async {
      await push.onSignedIn();
      source.tokenFails = Exception('firebaseinstallations.googleapis.com unreachable');

      expect(await push.enable(), PushStatus.unreachable);
      expect(api.registered, isEmpty);
    });

    test('allowed, but the server did not take the device: unreachable too', () async {
      await push.onSignedIn();
      api.failWith = Exception('offline');

      expect(await push.enable(), PushStatus.unreachable);
    });

    test('trying again once the network allows it registers, without asking again', () async {
      await push.onSignedIn();
      source.tokenFails = Exception('unreachable');
      await push.enable();

      source.tokenFails = null;
      expect(await push.enable(), PushStatus.enabled);

      expect(api.registered, ['token-1@Web']);
      expect(source.current, PushPermission.granted);
    });
  });

  group('status, for the notifications row', () {
    test('a build without push says so, and turning it on does nothing', () async {
      source.available = false;

      expect(await push.status(), PushStatus.unavailable);
      expect(await push.enable(), PushStatus.unavailable);
      expect(source.prompts, 0);
    });

    test('never asked', () async {
      expect(await push.status(), PushStatus.notAsked);
    });

    test('allowed', () async {
      source.current = PushPermission.granted;

      expect(await push.status(), PushStatus.enabled);
    });

    test('blocked', () async {
      source.current = PushPermission.denied;

      expect(await push.status(), PushStatus.blocked);
    });

    test('asking the status never prompts', () async {
      source.browser = true;

      await push.status();

      expect(source.prompts, 0);
    });
  });

  test('NoPush reports push as unavailable, so no row or card is ever offered', () async {
    const none = NoPush();

    expect(await none.status(), PushStatus.unavailable);
    expect(await none.enable(), PushStatus.unavailable);
  });
}
