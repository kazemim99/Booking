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
  bool permission = true;
  String? token = 'token-1';
  int deleted = 0;
  final refreshes = StreamController<String>.broadcast();

  @override
  Future<bool> isAvailable() async => available;

  @override
  Future<bool> requestPermission() async => permission;

  @override
  Future<String?> getToken() async => token;

  @override
  Stream<String> get onTokenRefresh => refreshes.stream;

  @override
  Future<void> deleteToken() async => deleted++;

  @override
  String get platform => 'Android';
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
}
