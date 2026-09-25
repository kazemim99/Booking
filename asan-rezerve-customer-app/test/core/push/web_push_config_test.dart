import 'package:asan_rezerve_customer_app/core/push/web_push_config.dart';
import 'package:flutter_test/flutter_test.dart';

/// Browser push is configured at BUILD time: CI passes the Firebase Web app's config and the VAPID public key as
/// --dart-define values. A build without them must behave exactly as before web push existed — no Firebase load, no
/// prompt — so "configured" means every value is present, never "some of them".
void main() {
  const complete = WebPushConfig(
    apiKey: 'api-key',
    appId: '1:123:web:abc',
    messagingSenderId: '123',
    projectId: 'asanrezerve-demo',
    vapidKey: 'BPublicVapidKey',
  );

  test('a build given every value is configured', () {
    expect(complete.isComplete, isTrue);
  });

  test('a build given none of them is not — the default, and what every build without CI secrets gets', () {
    expect(WebPushConfig.fromEnvironment.isComplete, isFalse);
  });

  test('one missing value is not configured: half a Firebase config fails in the browser, not at build time', () {
    for (final partial in [
      complete.copyWith(apiKey: ''),
      complete.copyWith(appId: ''),
      complete.copyWith(messagingSenderId: ''),
      complete.copyWith(projectId: ''),
      complete.copyWith(vapidKey: ''),
    ]) {
      expect(partial.isComplete, isFalse);
    }
  });

  test('whitespace is not a value (an unset CI variable can arrive as a blank)', () {
    expect(complete.copyWith(vapidKey: '  ').isComplete, isFalse);
  });

  test('the Firebase options are the Web app\'s own', () {
    final options = complete.toFirebaseOptions();

    expect(options.apiKey, 'api-key');
    expect(options.appId, '1:123:web:abc');
    expect(options.messagingSenderId, '123');
    expect(options.projectId, 'asanrezerve-demo');
  });

  group('service worker address', () {
    test('lives under push/, so it never takes the scope of the app itself', () {
      // Flutter's loader re-registers its own worker at "/" whenever a registration covers the page; a push worker
      // there would be replaced on the next load.
      expect(complete.serviceWorkerPath, startsWith('push/firebase-messaging-sw.js?'));
    });

    test('carries the config, which a service worker cannot read from dart-defines', () {
      final query = Uri.parse(complete.serviceWorkerPath).queryParameters;

      expect(query, {
        'apiKey': 'api-key',
        'appId': '1:123:web:abc',
        'messagingSenderId': '123',
        'projectId': 'asanrezerve-demo',
      });
    });

    test('does not carry the VAPID key: the page subscribes, the worker never needs it', () {
      expect(complete.serviceWorkerPath, isNot(contains('BPublicVapidKey')));
    });

    test('is encoded, so a value with reserved characters survives', () {
      final odd = complete.copyWith(appId: '1:123:web:a&b=c');

      expect(Uri.parse(odd.serviceWorkerPath).queryParameters['appId'], '1:123:web:a&b=c');
    });
  });
}
