import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Encrypted storage for tokens + provider session identity
/// (AUTH_SPECIFICATION.md §10.1 — Flutter uses secure storage, not localStorage).
///
/// Backed by a write-through in-memory cache: within a running session the
/// cache is authoritative and the underlying secure store is best-effort
/// persistence. This keeps auth working when the platform's secure storage is
/// unreliable — notably `flutter_secure_storage` on web, whose reads can throw
/// or return null, which otherwise breaks the AuthInterceptor's token lookup
/// (every authenticated request fails before it is sent) and session restore.
class SecureStorageService {
  final FlutterSecureStorage _storage;
  final Map<String, String?> _cache = {};

  SecureStorageService(this._storage);

  static const _accessTokenKey = 'access_token';
  static const _refreshTokenKey = 'refresh_token';
  static const _userIdKey = 'user_id';
  static const _providerIdKey = 'provider_id';
  static const _providerStatusKey = 'provider_status';
  static const _phoneNumberKey = 'phone_number';

  /// Write-through: cache first (authoritative for this session), then persist
  /// best-effort. Storage failures never propagate.
  Future<void> _write(String key, String? value) async {
    _cache[key] = value;
    try {
      if (value == null) {
        await _storage.delete(key: key);
      } else {
        await _storage.write(key: key, value: value);
      }
    } catch (_) {
      // Best-effort persistence; the in-memory cache holds the value.
    }
  }

  /// Cache-first read; falls back to the secure store (populating the cache),
  /// and degrades to null rather than throwing.
  Future<String?> _read(String key) async {
    if (_cache.containsKey(key)) return _cache[key];
    try {
      final value = await _storage.read(key: key);
      _cache[key] = value;
      return value;
    } catch (_) {
      return null;
    }
  }

  Future<void> saveAccessToken(String token) => _write(_accessTokenKey, token);
  Future<String?> getAccessToken() => _read(_accessTokenKey);

  Future<void> saveRefreshToken(String token) =>
      _write(_refreshTokenKey, token);
  Future<String?> getRefreshToken() => _read(_refreshTokenKey);

  Future<String?> getUserId() => _read(_userIdKey);
  Future<String?> getProviderId() => _read(_providerIdKey);
  Future<String?> getProviderStatus() => _read(_providerStatusKey);
  Future<String?> getPhoneNumber() => _read(_phoneNumberKey);

  Future<bool> isLoggedIn() async {
    final token = await getAccessToken();
    return token != null && token.isNotEmpty;
  }

  Future<void> saveSession({
    required String accessToken,
    required String refreshToken,
    required String userId,
    String? providerId,
    String? providerStatus,
    String? phoneNumber,
  }) async {
    await Future.wait([
      _write(_accessTokenKey, accessToken),
      _write(_refreshTokenKey, refreshToken),
      _write(_userIdKey, userId),
      _write(_providerIdKey, providerId),
      _write(_providerStatusKey, providerStatus),
      if (phoneNumber != null) _write(_phoneNumberKey, phoneNumber),
    ]);
  }

  /// Updates only the provider status/id (e.g. after onboarding).
  Future<void> saveProviderState({
    String? providerId,
    String? providerStatus,
  }) async {
    await Future.wait([
      _write(_providerIdKey, providerId),
      _write(_providerStatusKey, providerStatus),
    ]);
  }

  Future<void> clearSession() async {
    await Future.wait([
      _write(_accessTokenKey, null),
      _write(_refreshTokenKey, null),
      _write(_userIdKey, null),
      _write(_providerIdKey, null),
      _write(_providerStatusKey, null),
      _write(_phoneNumberKey, null),
    ]);
  }
}
