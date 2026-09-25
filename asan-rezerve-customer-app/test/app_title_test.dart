import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/main.dart' show appTitleFor;
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter_test/flutter_test.dart';

/// MaterialApp.title is the browser tab's title on the web, but on Android it is the label in the recents screen,
/// where "<name> | <tagline>" is cut off. The long document title belongs to the web only.
void main() {
  test('on the web the title is the long document title (the same text index.html shows before the app runs)', () {
    expect(appTitleFor(isWeb: true), AppStrings.appDocumentTitle);
  });

  test('on Android and iOS the title is the short app name', () {
    expect(appTitleFor(isWeb: false), AppStrings.homeTitle);
    expect(appTitleFor(isWeb: false), isNot(contains(AppStrings.appTagline)));
  });

  test('defaults to the platform the app runs on', () {
    expect(appTitleFor(), appTitleFor(isWeb: kIsWeb));
  });
}
