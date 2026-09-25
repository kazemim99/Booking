import 'package:asan_rezerve_provider_app/core/api/models/api_response.dart';
import 'package:asan_rezerve_provider_app/core/storage/secure_storage_service.dart';
import 'package:asan_rezerve_provider_app/features/auth/data/datasources/auth_api_service.dart';
import 'package:asan_rezerve_provider_app/features/auth/data/models/auth_models.dart';
import 'package:asan_rezerve_provider_app/features/auth/data/repositories/auth_repository_impl.dart';
import 'package:asan_rezerve_provider_app/features/auth/domain/entities/provider_status.dart';
import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

/// An unsigned JWT carrying [payload] — the app only reads claims.
String _token(Map<String, dynamic> payload) {
  String seg(Map<String, dynamic> m) =>
      base64Url.encode(utf8.encode(jsonEncode(m))).replaceAll('=', '');
  return '${seg({'alg': 'HS256', 'typ': 'JWT'})}.${seg(payload)}.sig';
}

const _givenName = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname';
const _surname = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname';
const _name = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';

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
    registerFallbackValue(const RefreshTokenRequest(refreshToken: '0'));
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

  // Production QA 2026-09-23: after a restart the header read «09123135143» — a restored session carried no name,
  // and the display fell back to the phone. The token has the name; the session takes it from there.
  group('the name comes from the token', () {
    void storedSession(String accessToken) {
      when(() => storage.getAccessToken()).thenAnswer((_) async => accessToken);
      when(() => storage.getRefreshToken()).thenAnswer((_) async => 'refresh');
      when(() => storage.getUserId()).thenAnswer((_) async => 'u-1');
      when(() => storage.getPhoneNumber()).thenAnswer((_) async => '09123135143');
      when(() => storage.getProviderStatus()).thenAnswer((_) async => 'Active');
      when(() => storage.getProviderId()).thenAnswer((_) async => 'p-1');
    }

    test('a cold start restores the real name', () async {
      storedSession(_token({_givenName: 'مصطفی', _surname: 'کاظمی', _name: 'مصطفی کاظمی'}));

      final session = (await repo.getCurrentSession())
          .getOrElse(() => throw StateError('expected Right'))!;

      expect(session.user.realName, 'مصطفی کاظمی');
    });

    test('a cold start with the placeholder has no name, and never the phone', () async {
      storedSession(_token({_givenName: 'ارائه‌دهنده', _surname: '9123135143', _name: 'ارائه‌دهنده 9123135143'}));

      final session = (await repo.getCurrentSession())
          .getOrElse(() => throw StateError('expected Right'))!;

      expect(session.user.realName, isNull);
    });

    test('a refreshed token after a rename carries the new name into the session', () async {
      when(() => api.completeProviderAuth(any())).thenAnswer((_) async => authOk());
      await repo.completeProviderAuthentication(phoneNumber: '09123135143', code: '123456');

      final renamed = _token({_givenName: 'مصطفی', _surname: 'کاظمی', _name: 'مصطفی کاظمی'});
      when(() => storage.getRefreshToken()).thenAnswer((_) async => 'refresh');
      when(() => storage.saveAccessToken(any())).thenAnswer((_) async {});
      when(() => storage.saveRefreshToken(any())).thenAnswer((_) async {});
      when(() => api.refreshToken(any())).thenAnswer((_) async => ApiResponse<RefreshTokenResponse>(
            success: true,
            data: RefreshTokenResponse(accessToken: renamed, refreshToken: 'refresh-2', expiresIn: 86400),
          ));

      final refreshed = (await repo.refreshToken()).getOrElse(() => throw StateError('expected Right'));
      expect(refreshed.user.realName, 'مصطفی کاظمی');

      final current = (await repo.getCurrentSession()).getOrElse(() => throw StateError('expected Right'))!;
      expect(current.user.realName, 'مصطفی کاظمی', reason: 'the header re-reads this session');
    });

    // QA 2026-09-24: onboarding asks the owner's name and the server saves it on their account, but the app kept
    // the sign-in token (placeholder name) and then asked «نام شما ثبت نشده» — for a name it had just been given.
    test('after onboarding, the refreshed session carries the name onboarding gave', () async {
      final placeholder =
          _token({_givenName: 'ارائه‌دهنده', _surname: '9123135143', _name: 'ارائه‌دهنده 9123135143'});
      when(() => api.completeProviderAuth(any())).thenAnswer((_) async => ApiResponse<CompleteProviderAuthResponse>(
            success: true,
            data: CompleteProviderAuthResponse(
              isNewProvider: true,
              userId: 'u-1',
              phoneNumber: '09123135143',
              fullName: 'ارائه‌دهنده 9123135143',
              accessToken: placeholder,
              refreshToken: 'refresh',
              expiresIn: 86400,
              requiresOnboarding: true,
              message: 'ok',
            ),
          ));
      await repo.completeProviderAuthentication(phoneNumber: '09123135143', code: '123456');

      final named = _token({_givenName: 'مصطفی', _surname: 'کاظمی', _name: 'مصطفی کاظمی'});
      when(() => storage.getRefreshToken()).thenAnswer((_) async => 'refresh');
      when(() => storage.saveAccessToken(any())).thenAnswer((_) async {});
      when(() => storage.saveRefreshToken(any())).thenAnswer((_) async {});
      when(() => storage.saveProviderState(
            providerId: any(named: 'providerId'),
            providerStatus: any(named: 'providerStatus'),
          )).thenAnswer((_) async {});
      when(() => api.refreshToken(any())).thenAnswer((_) async => ApiResponse<RefreshTokenResponse>(
            success: true,
            data: RefreshTokenResponse(accessToken: named, refreshToken: 'refresh-2', expiresIn: 86400),
          ));
      when(() => api.getCurrentProviderStatus())
          .thenAnswer((_) async => (providerId: 'p-1', status: 'PendingVerification'));

      final session =
          (await repo.refreshProviderStatus()).getOrElse(() => throw StateError('expected Right'));

      expect(session.user.realName, 'مصطفی کاظمی');
    });
  });
}
