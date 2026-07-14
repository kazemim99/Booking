import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Encrypted storage for tokens + provider session identity
/// (AUTH_SPECIFICATION.md §10.1 — Flutter uses secure storage, not localStorage).
class SecureStorageService {
  final FlutterSecureStorage _storage;

  SecureStorageService(this._storage);

  static const _accessTokenKey = 'access_token';
  static const _refreshTokenKey = 'refresh_token';
  static const _userIdKey = 'user_id';
  static const _providerIdKey = 'provider_id';
  static const _providerStatusKey = 'provider_status';
  static const _phoneNumberKey = 'phone_number';

  Future<void> saveAccessToken(String token) =>
      _storage.write(key: _accessTokenKey, value: token);
  Future<String?> getAccessToken() => _storage.read(key: _accessTokenKey);

  Future<void> saveRefreshToken(String token) =>
      _storage.write(key: _refreshTokenKey, value: token);
  Future<String?> getRefreshToken() => _storage.read(key: _refreshTokenKey);

  Future<String?> getUserId() => _storage.read(key: _userIdKey);
  Future<String?> getProviderId() => _storage.read(key: _providerIdKey);
  Future<String?> getProviderStatus() => _storage.read(key: _providerStatusKey);
  Future<String?> getPhoneNumber() => _storage.read(key: _phoneNumberKey);

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
      saveAccessToken(accessToken),
      saveRefreshToken(refreshToken),
      _storage.write(key: _userIdKey, value: userId),
      _storage.write(key: _providerIdKey, value: providerId),
      _storage.write(key: _providerStatusKey, value: providerStatus),
      if (phoneNumber != null)
        _storage.write(key: _phoneNumberKey, value: phoneNumber),
    ]);
  }

  /// Updates only the provider status/id (e.g. after onboarding).
  Future<void> saveProviderState({
    String? providerId,
    String? providerStatus,
  }) async {
    await Future.wait([
      _storage.write(key: _providerIdKey, value: providerId),
      _storage.write(key: _providerStatusKey, value: providerStatus),
    ]);
  }

  Future<void> clearSession() async {
    await Future.wait([
      _storage.delete(key: _accessTokenKey),
      _storage.delete(key: _refreshTokenKey),
      _storage.delete(key: _userIdKey),
      _storage.delete(key: _providerIdKey),
      _storage.delete(key: _providerStatusKey),
      _storage.delete(key: _phoneNumberKey),
    ]);
  }
}
