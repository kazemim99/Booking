import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

/// Guards the Android manifest declarations that the checkout journey depends on.
///
/// These are configuration, not Dart, so no widget or bloc test can catch a regression here — a deleted
/// `<queries>` entry would compile, analyze and unit-test perfectly while leaving checkout unable to start on a
/// real device. This test reads the manifest directly so the guarantee is pinned somewhere `flutter test` runs.
void main() {
  late String manifest;

  setUpAll(() {
    final file = File('android/app/src/main/AndroidManifest.xml');
    expect(
      file.existsSync(),
      isTrue,
      reason: 'Expected the Android manifest at ${file.path} (run `flutter test` from the package root)',
    );
    manifest = file.readAsStringSync();
  });

  /// The `<queries>` element, with comments stripped so commented-out declarations never count as present.
  String queriesBlock() {
    final withoutComments = manifest.replaceAll(RegExp(r'<!--.*?-->', dotAll: true), '');
    final match = RegExp(r'<queries>(.*?)</queries>', dotAll: true).firstMatch(withoutComments);
    expect(match, isNotNull, reason: 'AndroidManifest.xml declares no <queries> element');
    return match!.group(1)!;
  }

  group('package visibility (<queries>)', () {
    test('declares an https VIEW intent so the payment gateway can be opened', () {
      final queries = queriesBlock();

      // Android 11+ (this app targets SDK 36) hides other packages unless a matching intent is declared. Without
      // this, url_launcher cannot resolve a browser and LaunchMode.externalApplication fails — checkout never
      // reaches the gateway.
      final intents = RegExp(r'<intent>(.*?)</intent>', dotAll: true)
          .allMatches(queries)
          .map((m) => m.group(1)!);

      final hasHttpsView = intents.any((intent) =>
          intent.contains('android.intent.action.VIEW') &&
          RegExp(r'''android:scheme\s*=\s*["']https["']''').hasMatch(intent));

      expect(
        hasHttpsView,
        isTrue,
        reason: 'The <queries> block must declare an <intent> with action VIEW and data scheme "https", '
            'otherwise url_launcher cannot open the ZarinPal payment page on Android 11+',
      );
    });

    test('keeps the pre-existing PROCESS_TEXT declaration', () {
      // Used by the Flutter engine's text-processing plugin; removing it would be an unrelated regression.
      expect(queriesBlock(), contains('android.intent.action.PROCESS_TEXT'));
    });
  });

  group('deep linking stays deferred', () {
    test('no BROWSABLE intent-filter is declared', () {
      // The approved architecture is: gateway → server-side callback/verification → Vue return page → app
      // foreground resume/verify. App Links are a deferred UX enhancement, not a correctness requirement.
      // If that decision is ever revisited, this test should be updated deliberately — not tripped over.
      final withoutComments = manifest.replaceAll(RegExp(r'<!--.*?-->', dotAll: true), '');

      expect(
        withoutComments.contains('android.intent.category.BROWSABLE'),
        isFalse,
        reason: 'A BROWSABLE intent-filter means inbound deep links were added; that decision is currently '
            'deferred, so this needs an explicit revisit rather than an incidental change',
      );
    });

    test('the launcher activity still declares exactly one intent-filter', () {
      final withoutComments = manifest.replaceAll(RegExp(r'<!--.*?-->', dotAll: true), '');
      final filters = RegExp(r'<intent-filter>', dotAll: true).allMatches(withoutComments).length;

      expect(
        filters,
        1,
        reason: 'Only the MAIN/LAUNCHER filter is expected; an extra filter implies inbound routing was added',
      );
    });
  });
}
