import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/core/storage/secure_storage_service.dart';

/// Regression cover for a defect that surfaced two different ways in the running app:
/// the splash screen hanging forever, and the home page showing the raw platform error
/// "OperationError: خطا در بارگذاری داده‌ها" instead of a real message.
///
/// Root cause: on Flutter web, `flutter_secure_storage` decrypts each value with the
/// Web Crypto API using a key tied to the browser session/origin. A value written by an
/// earlier build/session can fail to decrypt after a rebuild, and the platform throws a
/// `DOMException` ("OperationError") from `read()` instead of returning null. Every
/// caller in this app expects "no value stored" to come back as null, not as an
/// exception — so an unhandled throw here breaks whichever caller happens to read it.
///
/// `SecureStorageService.readSafely` is the fix: any read failure is treated as "value
/// absent" (matching how the app already treats a signed-out user) rather than an error,
/// and the unreadable key is deleted so the failure does not repeat on the next read.
void main() {
  group('SecureStorageService.readSafely', () {
    test('returns the value on a normal successful read', () async {
      final result = await SecureStorageService.readSafely(
        read: () async => 'a-real-token',
        delete: () async => fail('must not delete on a successful read'),
      );

      expect(result, 'a-real-token');
    });

    test('returns null, unmodified, when nothing is stored', () async {
      final result = await SecureStorageService.readSafely(
        read: () async => null,
        delete: () async => fail('must not delete when there was nothing to delete'),
      );

      expect(result, isNull);
    });

    test(
      'returns null instead of throwing when the read fails '
      '(the exact defect: a DOMException("OperationError") from a stale encrypted value)',
      () async {
        final result = await SecureStorageService.readSafely(
          read: () async => throw Exception('OperationError'),
          delete: () async {},
        );

        expect(result, isNull);
      },
    );

    test('deletes the key once it is confirmed unreadable, so the failure self-heals', () async {
      var deleteCalled = false;

      await SecureStorageService.readSafely(
        read: () async => throw Exception('OperationError'),
        delete: () async => deleteCalled = true,
      );

      expect(deleteCalled, isTrue,
          reason: 'a value that can never be read again must not be left behind to fail every future read');
    });

    test('still returns null even if the cleanup delete itself fails', () async {
      final result = await SecureStorageService.readSafely(
        read: () async => throw Exception('OperationError'),
        delete: () async => throw Exception('delete also failed'),
      );

      expect(result, isNull,
          reason: 'best-effort cleanup failing must not turn "value absent" back into an exception');
    });
  });
}
