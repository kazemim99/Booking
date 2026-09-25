import 'package:booksy_provider_app/features/auth/data/models/auth_models.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_status.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('SendVerificationCodeRequest (E-1 wire invariant)', () {
    test('defaults countryCode to non-digit "IR" so backend Value matches', () {
      final json = const SendVerificationCodeRequest(phoneNumber: '09121234567')
          .toJson();
      expect(json['phoneNumber'], '09121234567');
      // MUST be a non-digit marker; sending "+98" would store "+9809..." and
      // break the complete-auth lookup (AUTH_SPECIFICATION.md §4.1 / E-1).
      expect(json['countryCode'], 'IR');
      expect(RegExp(r'\d').hasMatch(json['countryCode'] as String), isFalse);
    });
  });

  group('CompleteProviderAuthResponse.toSession', () {
    test('maps active provider fields', () {
      final dto = CompleteProviderAuthResponse.fromJson({
        'isNewProvider': false,
        'userId': 'u1',
        'providerId': 'p1',
        'providerStatus': 'Active',
        'phoneNumber': '09121234567',
        'email': 'a@b.com',
        'fullName': 'رضا محمدی',
        'accessToken': 'access',
        'refreshToken': 'refresh',
        'expiresIn': 86400,
        'requiresOnboarding': false,
        'message': 'ok',
      });
      final session = dto.toSession();

      expect(session.accessToken, 'access');
      expect(session.providerId, 'p1');
      expect(session.providerStatus, ProviderStatus.active);
      expect(session.isNewProvider, isFalse);
      expect(session.needsOnboarding, isFalse);
      expect(session.isBlocked, isFalse);
      expect(session.user.firstName, 'رضا');
      expect(session.user.lastName, 'محمدی');
    });

    test('new provider with null profile → needs onboarding', () {
      final dto = CompleteProviderAuthResponse.fromJson({
        'isNewProvider': true,
        'userId': 'u2',
        'providerId': null,
        'providerStatus': null,
        'phoneNumber': '09121234567',
        'fullName': '',
        'accessToken': 'a',
        'refreshToken': 'r',
        'expiresIn': 86400,
        'requiresOnboarding': true,
        'message': 'welcome',
      });
      final session = dto.toSession();
      expect(session.isNewProvider, isTrue);
      expect(session.providerStatus, isNull);
      expect(session.needsOnboarding, isTrue);
    });
  });
}
