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

  // ==================== Token Management ====================

  /// Save access token
  Future<void> saveAccessToken(String token) async {
    await _secureStorage.write(key: _accessTokenKey, value: token);
  }

  /// Get access token
  Future<String?> getAccessToken() async {
    return await _secureStorage.read(key: _accessTokenKey);
  }

  /// Save refresh token
  Future<void> saveRefreshToken(String token) async {
    await _secureStorage.write(key: _refreshTokenKey, value: token);
  }

  /// Get refresh token
  Future<String?> getRefreshToken() async {
    return await _secureStorage.read(key: _refreshTokenKey);
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
    return await _secureStorage.read(key: _userIdKey);
  }

  /// Save customer ID
  Future<void> saveCustomerId(String customerId) async {
    await _secureStorage.write(key: _customerIdKey, value: customerId);
  }

  /// Get customer ID
  Future<String?> getCustomerId() async {
    return await _secureStorage.read(key: _customerIdKey);
  }

  /// Save phone number
  Future<void> savePhoneNumber(String phoneNumber) async {
    await _secureStorage.write(key: _phoneNumberKey, value: phoneNumber);
  }

  /// Get phone number
  Future<String?> getPhoneNumber() async {
    return await _secureStorage.read(key: _phoneNumberKey);
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
    return await _secureStorage.read(key: key);
  }

  /// Delete generic value
  Future<void> delete(String key) async {
    await _secureStorage.delete(key: key);
  }
}
