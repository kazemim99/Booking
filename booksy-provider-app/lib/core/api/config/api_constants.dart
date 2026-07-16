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

  /// Public customer-facing profile URL for a provider (the shareable booking
  /// link). Host mirrors the deployed Vue frontend.
  static String publicProviderUrl(String providerId) =>
      'https://napstar.ir/providers/$providerId';

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

  // ==================== Bookings (ServiceCatalog) ====================

  /// GET — provider's bookings (query: status/from/to). Policy ProviderOrAdmin.
  static String providerBookings(String providerId) =>
      '/$apiVersion/Bookings/provider/$providerId';

  /// GET — booking statistics (query: providerId/startDate/endDate).
  static const String bookingStatistics = '/$apiVersion/Bookings/statistics';

  /// GET — the provider's services (paged; used to resolve service names).
  static String providerServices(String providerId) =>
      '/$apiVersion/Services/provider/$providerId';

  /// GET — the provider's client book (derived from bookings).
  static String providerClients(String providerId) =>
      '/$apiVersion/Providers/$providerId/clients';

  /// GET — the provider's staff members.
  static String providerStaff(String providerId) =>
      '/$apiVersion/Providers/$providerId/staff';

  /// GET — available time slots (query: providerId/serviceId/date[/staffId]).
  static const String availableSlots = '/$apiVersion/Bookings/available-slots';

  /// POST — create a booking (customer = the JWT caller).
  static const String bookings = '/$apiVersion/Bookings';

  /// POST — provider confirms a pending booking request.
  static String bookingConfirm(String id) => '/$apiVersion/Bookings/$id/confirm';

  /// POST — cancel/decline a booking (body: reason/cancelledBy).
  static String bookingCancel(String id) => '/$apiVersion/Bookings/$id/cancel';

  /// POST — mark a booking completed.
  static String bookingComplete(String id) =>
      '/$apiVersion/Bookings/$id/complete';

  /// POST — mark a booking as a client no-show.
  static String bookingNoShow(String id) => '/$apiVersion/Bookings/$id/no-show';

  // ==================== Onboarding (ServiceCatalog) ====================

  /// POST — create the organization draft (onboarding step 3).
  static const String registerOrganization =
      '/$apiVersion/Providers/organizations';

  /// GET — current registration progress (draft restore).
  static const String registrationProgress = '/$apiVersion/Registration/progress';

  /// POST — save services (onboarding step 4).
  static const String registrationServices =
      '/$apiVersion/Registration/step-4/services';

  /// POST — save working hours (onboarding step 6).
  static const String registrationWorkingHours =
      '/$apiVersion/Registration/step-6/working-hours';

  /// POST — upload gallery images (onboarding step 7, optional, multipart).
  static const String registrationGallery =
      '/$apiVersion/Registration/step-7/gallery';

  /// POST — complete registration (onboarding step 9).
  static const String registrationComplete =
      '/$apiVersion/Registration/step-9/complete';
}
