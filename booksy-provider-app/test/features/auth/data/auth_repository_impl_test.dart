import 'package:booksy_provider_app/core/api/models/api_response.dart';
import 'package:booksy_provider_app/core/storage/secure_storage_service.dart';
import 'package:booksy_provider_app/features/auth/data/datasources/auth_api_service.dart';
import 'package:booksy_provider_app/features/auth/data/models/auth_models.dart';
import 'package:booksy_provider_app/features/auth/data/repositories/auth_repository_impl.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_status.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class MockAuthApiService extends Mock implements AuthApiService {}

class MockSecureStorageService extends Mock implements SecureStorageService {}

void main() {
  late MockAuthApiService api;
  late MockSecureStorageService storage;
  late AuthRepositoryImpl repo;

  ApiResponse<CompleteProviderAuthResponse> authOk() =>
      ApiResponse<CompleteProviderAuthResponse>(
        success: true,
        data: const CompleteProviderAuthResponse(
          isNewProvider: false,
          userId: 'u-1',
          providerId: 'p-1',
          providerStatus: 'PendingVerification',
          phoneNumber: '09123135143',
          fullName: 'سالن رُز',
          accessToken: 'access',
          refreshToken: 'refresh',
          expiresIn: 86400,
          requiresOnboarding: false,
          message: 'ok',
        ),
      );

  setUpAll(() {
    registerFallbackValue(
      const CompleteProviderAuthRequest(phoneNumber: '0', code: '0'),
    );
  });

  setUp(() {
    api = MockAuthApiService();
    storage = MockSecureStorageService();
    repo = AuthRepositoryImpl(api, storage);
    when(() => storage.saveSession(
          accessToken: any(named: 'accessToken'),
          refreshToken: any(named: 'refreshToken'),
          userId: any(named: 'userId'),
          providerId: any(named: 'providerId'),
          providerStatus: any(named: 'providerStatus'),
          phoneNumber: any(named: 'phoneNumber'),
        )).thenAnswer((_) async {});
    when(() => storage.clearSession()).thenAnswer((_) async {});
  });

  test(
      'after login, getCurrentSession returns the cached session without '
      'reading storage (survives web storage round-trip failure)', () async {
    when(() => api.completeProviderAuth(any()))
        .thenAnswer((_) async => authOk());

    final login = await repo.completeProviderAuthentication(
        phoneNumber: '09123135143', code: '123456');
    expect(login.isRight(), isTrue);

    final result = await repo.getCurrentSession();
    final session =
        result.getOrElse(() => throw StateError('expected Right'));

    expect(session!.providerId, 'p-1');
    expect(session.providerStatus, ProviderStatus.pendingVerification);
    // The crux: no storage reads — the in-memory session is authoritative.
    verifyNever(() => storage.getAccessToken());
    verifyNever(() => storage.getProviderId());
    verifyNever(() => storage.getProviderStatus());
  });

  test('cold start (no prior auth) falls back to storage', () async {
    when(() => storage.getAccessToken()).thenAnswer((_) async => 'access');
    when(() => storage.getRefreshToken()).thenAnswer((_) async => 'refresh');
    when(() => storage.getUserId()).thenAnswer((_) async => 'u-1');
    when(() => storage.getPhoneNumber())
        .thenAnswer((_) async => '09123135143');
    when(() => storage.getProviderStatus())
        .thenAnswer((_) async => 'PendingVerification');
    when(() => storage.getProviderId()).thenAnswer((_) async => 'p-1');

    final result = await repo.getCurrentSession();
    final session =
        result.getOrElse(() => throw StateError('expected Right'));

    expect(session!.providerId, 'p-1');
    verify(() => storage.getProviderId()).called(1);
  });

  test('logout clears the cache so getCurrentSession re-reads storage',
      () async {
    when(() => api.completeProviderAuth(any()))
        .thenAnswer((_) async => authOk());
    when(() => api.logout()).thenAnswer((_) async {});
    // Storage returns nothing after logout → null session.
    when(() => storage.getAccessToken()).thenAnswer((_) async => null);
    when(() => storage.getRefreshToken()).thenAnswer((_) async => null);
    when(() => storage.getUserId()).thenAnswer((_) async => null);
    when(() => storage.getPhoneNumber()).thenAnswer((_) async => null);

    await repo.completeProviderAuthentication(
        phoneNumber: '09123135143', code: '123456');
    await repo.logout();

    final result = await repo.getCurrentSession();
    expect(result.getOrElse(() => throw StateError('expected Right')), isNull);
    verify(() => storage.getAccessToken()).called(1); // cache was cleared
  });

  group('switchActiveOrganization (salon switcher, WS6a)', () {
    setUp(() {
      when(() => storage.saveProviderState(
            providerId: any(named: 'providerId'),
            providerStatus: any(named: 'providerStatus'),
          )).thenAnswer((_) async {});
      when(() => storage.getAccessToken()).thenAnswer((_) async => 'access');
      when(() => storage.getRefreshToken()).thenAnswer((_) async => 'refresh');
      when(() => storage.getUserId()).thenAnswer((_) async => 'u-1');
      when(() => storage.getPhoneNumber())
          .thenAnswer((_) async => '09123135143');
    });

    test('persists the chosen salon BEFORE asking the server, then returns a '
        'session scoped to it', () async {
      when(() => api.getCurrentProviderStatus())
          .thenAnswer((_) async => (providerId: 'p-2', status: 'Active'));
      when(() => storage.getProviderId()).thenAnswer((_) async => 'p-2');
      when(() => storage.getProviderStatus()).thenAnswer((_) async => 'Active');

      final result = await repo.switchActiveOrganization(providerId: 'p-2');

      final session = result.getOrElse(() => throw StateError('expected Right'));
      expect(session.providerId, 'p-2');
      expect(session.providerStatus, ProviderStatus.active);
      // The choice is written first so a crash or restart still lands in the
      // chosen salon, and only then is the status re-derived from the server.
      verifyInOrder([
        () => storage.saveProviderState(providerId: 'p-2', providerStatus: null),
        () => api.getCurrentProviderStatus(),
      ]);
    });

    test('when the status refresh fails the switch is reported as a failure '
        'and the previous in-memory session is kept', () async {
      when(() => api.completeProviderAuth(any()))
          .thenAnswer((_) async => authOk());
      await repo.completeProviderAuthentication(
          phoneNumber: '09123135143', code: '123456');
      when(() => api.getCurrentProviderStatus())
          .thenThrow(Exception('network'));

      final result = await repo.switchActiveOrganization(providerId: 'p-2');
      expect(result.isLeft(), isTrue);

      // The caller can retry; meanwhile the app still has a usable session.
      final current = await repo.getCurrentSession();
      final session = current.getOrElse(() => throw StateError('expected Right'));
      expect(session!.providerId, 'p-1');
      verifyNever(() => storage.getAccessToken());
    });
  });
}
