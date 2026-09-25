import 'package:asan_rezerve_customer_app/core/api/config/api_constants.dart';
import 'package:flutter_test/flutter_test.dart';

/// The web build talks to the deployed API only because CI compiles the URL in
/// (--dart-define=API_BASE_URL). Without that the bundle silently points at
/// localhost:5000 and every request fails in the browser — which is what the
/// provider app shipped once (openspec/changes/_inline/customer-sees-salon-bookings).
void main() {
  test('the base url is the compiled-in one when CI provides it', () {
    const override = String.fromEnvironment('API_BASE_URL');
    if (override.isEmpty) {
      // Plain `flutter test`: a local development default (localhost, or the
      // Android emulator's host alias), never a production URL.
      expect(ApiConstants.baseUrl, anyOf(contains('localhost'), contains('10.0.2.2')));
      expect(ApiConstants.baseUrl, isNot(contains('nahalkmi')));
    } else {
      expect(ApiConstants.baseUrl, override);
    }
  });
}
