import 'package:flutter/foundation.dart' show visibleForTesting;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:injectable/injectable.dart';

/// Secure Storage Service
/// Handles encrypted storage for sensitive data like tokens
@lazySingleton
class SecureStorageService {
  final FlutterSecureStorage _secureStorage;

  SecureStorageService(this._secureStorage);

  // Storage keys
  static const String _accessTokenKey = 'access_token';
  static const String _refreshTokenKey = 'refresh_token';
  static const String _userIdKey = 'user_id';
  static const String _customerIdKey = 'customer_id';
  static const String _phoneNumberKey = 'phone_number';

  /// Every read in this service goes through here rather than calling
  /// `_secureStorage.read` directly.
  ///
  /// On Flutter web, `flutter_secure_storage` encrypts values with the Web Crypto API
  /// (AES-GCM) using a key tied to the browser session/origin. A value written by an
  /// earlier build or session can fail to decrypt after a rebuild, and the platform
  /// throws a `DOMException` — `OperationError` — instead of the read simply
  /// returning null. Two different call sites (`AuthBloc.CheckAuthStatusEvent` and
  /// `GetHomeData`) each independently hit this and surfaced it two different ways:
  /// one hung the app on the splash screen forever, the other displayed
  /// "OperationError: ..." — a raw platform exception — as if it were a server error.
  ///
  /// An unreadable value is, from the app's perspective, indistinguishable from no
  /// value at all: both mean "we don't know who this is," which is a normal,
  /// recoverable, signed-out-like state — never a fatal error. So a failed read
  /// returns null here rather than letting the platform exception escape, and the
  /// unreadable key is proactively deleted so the same failure doesn't repeat on
  /// every subsequent read this session.
  Future<String?> _readSafely(String key) => readSafely(
        read: () => _secureStorage.read(key: key),
        delete: () => _secureStorage.delete(key: key),
      );

  /// The actual catch/recover logic, factored out so it can be exercised without a real
  /// `FlutterSecureStorage` (a concrete platform-channel class that cannot be faked in a
  /// plain unit test without a plugin like mockito). [read] and [delete] stand in for the
  /// plugin calls; production always passes the real ones via [_readSafely].
  @visibleForTesting
  static Future<String?> readSafely({
    required Future<String?> Function() read,
    required Future<void> Function() delete,
  }) async {
    try {
      return await read();
    } catch (_) {
      try {
        await delete();
      } catch (_) {
        // Deletion is best-effort cleanup; the read above already resolved to "absent"
        // regardless of whether this succeeds.
      }
      return null;
    }
  }

  // ==================== Token Management ====================

  /// Save access token
  Future<void> saveAccessToken(String token) async {
    await _secureStorage.write(key: _accessTokenKey, value: token);
  }

  /// Get access token
  Future<String?> getAccessToken() async {
    return _readSafely(_accessTokenKey);
  }

  /// Save refresh token
  Future<void> saveRefreshToken(String token) async {
    await _secureStorage.write(key: _refreshTokenKey, value: token);
  }

  /// Get refresh token
  Future<String?> getRefreshToken() async {
    return _readSafely(_refreshTokenKey);
  }

  /// Delete tokens
  Future<void> deleteTokens() async {
    await _secureStorage.delete(key: _accessTokenKey);
    await _secureStorage.delete(key: _refreshTokenKey);
  }

  // ==================== User Data ====================

  /// Save user ID
  Future<void> saveUserId(String userId) async {
    await _secureStorage.write(key: _userIdKey, value: userId);
  }

  /// Get user ID
  Future<String?> getUserId() async {
    return _readSafely(_userIdKey);
  }

  /// Save customer ID
  Future<void> saveCustomerId(String customerId) async {
    await _secureStorage.write(key: _customerIdKey, value: customerId);
  }

  /// Get customer ID
  Future<String?> getCustomerId() async {
    return _readSafely(_customerIdKey);
  }

  /// Save phone number
  Future<void> savePhoneNumber(String phoneNumber) async {
    await _secureStorage.write(key: _phoneNumberKey, value: phoneNumber);
  }

  /// Get phone number
  Future<String?> getPhoneNumber() async {
    return _readSafely(_phoneNumberKey);
  }

  // ==================== Session Management ====================

  /// Check if user is logged in
  Future<bool> isLoggedIn() async {
    final token = await getAccessToken();
    return token != null && token.isNotEmpty;
  }

  /// Save authentication session
  Future<void> saveAuthSession({
    required String accessToken,
    required String refreshToken,
    required String userId,
    required String customerId,
    String? phoneNumber,
  }) async {
    await Future.wait([
      saveAccessToken(accessToken),
      saveRefreshToken(refreshToken),
      saveUserId(userId),
      saveCustomerId(customerId),
      if (phoneNumber != null) savePhoneNumber(phoneNumber),
    ]);
  }

  /// Clear all authentication data
  Future<void> clearAuthSession() async {
    await Future.wait([
      deleteTokens(),
      _secureStorage.delete(key: _userIdKey),
      _secureStorage.delete(key: _customerIdKey),
      _secureStorage.delete(key: _phoneNumberKey),
    ]);
  }

  /// Clear all stored data
  Future<void> clearAll() async {
    await _secureStorage.deleteAll();
  }

  // ==================== Generic Storage ====================

  /// Save generic string value
  Future<void> saveString(String key, String value) async {
    await _secureStorage.write(key: key, value: value);
  }

  /// Get generic string value
  Future<String?> getString(String key) async {
    return _readSafely(key);
  }

  /// Delete generic value
  Future<void> delete(String key) async {
    await _secureStorage.delete(key: key);
  }
}
