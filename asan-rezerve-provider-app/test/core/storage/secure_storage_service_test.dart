import 'package:booksy_provider_app/core/storage/secure_storage_service.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class MockFlutterSecureStorage extends Mock implements FlutterSecureStorage {}

void main() {
  late MockFlutterSecureStorage backing;
  late SecureStorageService service;

  setUp(() {
    backing = MockFlutterSecureStorage();
    service = SecureStorageService(backing);
    when(() => backing.write(
        key: any(named: 'key'),
        value: any(named: 'value'))).thenAnswer((_) async {});
    when(() => backing.delete(key: any(named: 'key')))
        .thenAnswer((_) async {});
  });

  test(
      'reads survive a throwing backend — token comes from the write-through '
      'cache (the flutter_secure_storage-on-web failure mode)', () async {
    // Simulate web: every underlying read throws.
    when(() => backing.read(key: any(named: 'key')))
        .thenThrow(Exception('web secure storage unavailable'));

    await service.saveAccessToken('jwt-123');

    // Would have thrown before the cache fix; now returns the cached value,
    // so the AuthInterceptor can attach the token and requests go out.
    expect(await service.getAccessToken(), 'jwt-123');
    expect(await service.isLoggedIn(), isTrue);
  });

  test('cache-miss reads fall back to the backing store', () async {
    when(() => backing.read(key: any(named: 'key')))
        .thenAnswer((_) async => 'from-storage');

    // Cold start: nothing written this session → reads the store once, caches.
    expect(await service.getUserId(), 'from-storage');
    expect(await service.getUserId(), 'from-storage');
    verify(() => backing.read(key: any(named: 'key'))).called(1); // cached
  });

  test('clearSession makes subsequent reads return null from cache', () async {
    when(() => backing.read(key: any(named: 'key')))
        .thenThrow(Exception('unavailable'));
    await service.saveSession(
      accessToken: 'a',
      refreshToken: 'r',
      userId: 'u',
      providerId: 'p',
      providerStatus: 'Active',
      phoneNumber: '0912',
    );
    expect(await service.getProviderId(), 'p');

    await service.clearSession();

    expect(await service.getAccessToken(), isNull);
    expect(await service.getProviderId(), isNull);
    expect(await service.isLoggedIn(), isFalse);
  });
}
