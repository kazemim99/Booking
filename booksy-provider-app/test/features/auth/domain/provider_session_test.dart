import 'package:booksy_provider_app/features/auth/domain/entities/provider_session.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_status.dart';
import 'package:flutter_test/flutter_test.dart';

ProviderSession _session({
  String? providerId,
  ProviderStatus? status,
  bool isNew = false,
  bool requiresOnboarding = false,
}) {
  return ProviderSession(
    accessToken: 'a',
    refreshToken: 'r',
    expiresIn: 86400,
    user: const ProviderUser(id: 'u', phoneNumber: '09121234567', fullName: 'X'),
    providerId: providerId,
    providerStatus: status,
    isNewProvider: isNew,
    requiresOnboarding: requiresOnboarding,
  );
}

void main() {
  group('ProviderSession routing helpers', () {
    test('new provider needs onboarding', () {
      expect(_session(isNew: true).needsOnboarding, isTrue);
    });

    test('no provider profile needs onboarding', () {
      expect(_session(providerId: null, status: null).needsOnboarding, isTrue);
    });

    test('drafted status needs onboarding', () {
      expect(
        _session(providerId: 'p', status: ProviderStatus.drafted).needsOnboarding,
        isTrue,
      );
    });

    test('active provider does not need onboarding', () {
      expect(
        _session(providerId: 'p', status: ProviderStatus.active).needsOnboarding,
        isFalse,
      );
    });

    test('suspended provider is blocked (E-14)', () {
      expect(
        _session(providerId: 'p', status: ProviderStatus.suspended).isBlocked,
        isTrue,
      );
    });

    test('active provider is not blocked', () {
      expect(
        _session(providerId: 'p', status: ProviderStatus.active).isBlocked,
        isFalse,
      );
    });
  });

  // Production QA 2026-09-23: the header showed «09123135143» as the owner's name. The phone is not a name.
  group('ProviderUser.realName', () {
    test('a real name from the parts or the full name', () {
      expect(
        const ProviderUser(id: 'u', phoneNumber: '09123135143', firstName: 'مصطفی', lastName: 'کاظمی', fullName: '')
            .realName,
        'مصطفی کاظمی',
      );
      expect(
        const ProviderUser(id: 'u', phoneNumber: '09123135143', fullName: 'مصطفی کاظمی').realName,
        'مصطفی کاظمی',
      );
    });

    test('no name is null — never the phone number', () {
      expect(const ProviderUser(id: 'u', phoneNumber: '09123135143', fullName: '').realName, isNull);
    });

    test('the sign-in placeholder is no name', () {
      expect(
        const ProviderUser(
          id: 'u',
          phoneNumber: '09123135143',
          firstName: 'ارائه‌دهنده',
          lastName: '9123135143',
          fullName: 'ارائه‌دهنده 9123135143',
        ).realName,
        isNull,
      );
      expect(
        const ProviderUser(id: 'u', phoneNumber: '09123135143', fullName: 'ارائه‌دهنده 9123135143').realName,
        isNull,
      );
    });
  });
}
