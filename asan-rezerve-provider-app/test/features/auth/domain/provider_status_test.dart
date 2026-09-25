import 'package:booksy_provider_app/features/auth/domain/entities/provider_status.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('ProviderStatus', () {
    test('tryParse maps wire names', () {
      expect(ProviderStatus.tryParse('Drafted'), ProviderStatus.drafted);
      expect(ProviderStatus.tryParse('Active'), ProviderStatus.active);
      expect(ProviderStatus.tryParse('PendingVerification'),
          ProviderStatus.pendingVerification);
      expect(ProviderStatus.tryParse(null), isNull);
      expect(ProviderStatus.tryParse('Nonsense'), isNull);
    });

    test('needsOnboarding only for Drafted', () {
      expect(ProviderStatus.drafted.needsOnboarding, isTrue);
      expect(ProviderStatus.active.needsOnboarding, isFalse);
      expect(ProviderStatus.pendingVerification.needsOnboarding, isFalse);
    });

    test('canUseDashboard for verified/active/pendingVerification', () {
      expect(ProviderStatus.active.canUseDashboard, isTrue);
      expect(ProviderStatus.verified.canUseDashboard, isTrue);
      expect(ProviderStatus.pendingVerification.canUseDashboard, isTrue);
      expect(ProviderStatus.drafted.canUseDashboard, isFalse);
      expect(ProviderStatus.suspended.canUseDashboard, isFalse);
    });

    test('isBlocked for suspended/inactive/archived (E-14)', () {
      expect(ProviderStatus.suspended.isBlocked, isTrue);
      expect(ProviderStatus.inactive.isBlocked, isTrue);
      expect(ProviderStatus.archived.isBlocked, isTrue);
      expect(ProviderStatus.active.isBlocked, isFalse);
      expect(ProviderStatus.drafted.isBlocked, isFalse);
    });
  });
}
