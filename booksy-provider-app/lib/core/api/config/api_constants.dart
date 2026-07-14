import 'package:flutter/foundation.dart'
    show kIsWeb, defaultTargetPlatform, TargetPlatform;

/// API configuration for the Provider app.
/// Base URL resolves per platform; endpoints centralized (no hardcoded URLs).
class ApiConstants {
  ApiConstants._();

  /// Base host. Android emulator uses the 10.0.2.2 host alias.
  static String get baseUrl {
    if (kIsWeb) return 'http://localhost:5000';
    if (defaultTargetPlatform == TargetPlatform.android) {
      return 'http://10.0.2.2:5000';
    }
    return 'http://localhost:5000';
  }

  static String get userManagementBaseUrl => '$baseUrl/api';
  static String get serviceCatalogBaseUrl => '$baseUrl/api';

  static const String apiVersion = 'v1';

  static const Duration connectTimeout = Duration(seconds: 30);
  static const Duration receiveTimeout = Duration(seconds: 30);
  static const Duration sendTimeout = Duration(seconds: 30);

  static const String acceptLanguage = 'fa-IR';

  // ==================== Auth Endpoints ====================

  /// POST — send OTP. AUTH_SPECIFICATION.md §5.1
  static const String sendVerificationCode =
      '/$apiVersion/Auth/send-verification-code';

  /// POST — provider verify + login/register. §5.2
  static const String completeProviderAuth =
      '/$apiVersion/Auth/provider/complete-authentication';

  /// POST — refresh access token. §5.4
  static const String refreshToken = '/$apiVersion/Auth/refresh';

  /// POST — logout. §5.5
  static const String logout = '/$apiVersion/Auth/logout';

  // ==================== Provider (ServiceCatalog) ====================

  /// GET — current provider status. §5.6
  static const String providerCurrentStatus =
      '/$apiVersion/Providers/current/status';

  /// POST — mint a provider-claimed token after onboarding. §5.7
  static const String providerRefreshToken =
      '/$apiVersion/Providers/current/refresh-token';
}
