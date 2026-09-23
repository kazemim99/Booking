import 'package:booksy_customer_app/core/push/firebase_push_token_source.dart';
import 'package:booksy_customer_app/core/push/web_push_config.dart';
import 'package:flutter_test/flutter_test.dart';

/// The part of the Firebase source that decides before Firebase is touched. The rest needs a browser or a phone.
void main() {
  const unconfigured = WebPushConfig(apiKey: '', appId: '', messagingSenderId: '', projectId: '', vapidKey: '');
  const configured = WebPushConfig(
    apiKey: 'k',
    appId: '1:1:web:1',
    messagingSenderId: '1',
    projectId: 'p',
    vapidKey: 'v',
  );

  test('a web build without the Firebase Web config has no push, and never loads Firebase to find out', () async {
    // Loading Firebase in a browser fetches its SDK from www.gstatic.com; a build that cannot use it must not pay
    // for it — this is the behaviour every web build had before web push, and still has until CI sets the values.
    final source = FirebasePushTokenSource(web: unconfigured, isWeb: true);

    expect(await source.isAvailable(), isFalse);
  });

  test('in a browser, the prompt waits for a tap and the backend is told the device is Web', () {
    final source = FirebasePushTokenSource(web: configured, isWeb: true);

    expect(source.promptNeedsUserAction, isTrue);
    expect(source.platform, 'Web');
  });

  test('on a phone, nothing changes: asked at sign-in, registered as the phone it is', () {
    final source = FirebasePushTokenSource(web: unconfigured, isWeb: false);

    expect(source.promptNeedsUserAction, isFalse);
    expect(source.platform, isNot('Web'));
  });
}
