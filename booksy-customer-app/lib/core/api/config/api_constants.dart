import 'package:flutter/foundation.dart' show kIsWeb, defaultTargetPlatform, TargetPlatform;

/// API Constants
/// Centralized configuration for API endpoints and settings
class ApiConstants {
  // Prevent instantiation
  ApiConstants._();

  // Base URL, resolved per-platform:
  // - Web (Chrome, etc.): 'localhost' resolves directly to the host machine.
  // - Android Emulator: 10.0.2.2 is a special alias to the host machine's localhost
  //   (works regardless of VPN status on the host).
  // - iOS Simulator / desktop: 'localhost' resolves directly to the host machine.
  //
  // Alternatives:
  // - Physical Device: Use 'http://192.168.1.x:5000' (replace with your PC's IP)
  // - Production: Use 'http://napstar.ir'
  static String get baseUrl {
    if (kIsWeb) return 'http://localhost:5000';
    if (defaultTargetPlatform == TargetPlatform.android) return 'http://10.0.2.2:5000';
    return 'http://localhost:5000';
  }

  // Microservice URLs
  static String get userManagementBaseUrl => '$baseUrl/api';
  static String get serviceCatalogBaseUrl => '$baseUrl/api';

  // API Version
  static const String apiVersion = 'v1';

  // Timeouts
  static const Duration connectTimeout = Duration(seconds: 30);
  static const Duration receiveTimeout = Duration(seconds: 30);
  static const Duration sendTimeout = Duration(seconds: 30);

  // Headers
  static const String contentType = 'application/json';
  static const String acceptLanguage = 'fa-IR'; // Persian/Farsi

  // Storage Keys
  static const String accessTokenKey = 'access_token';
  static const String refreshTokenKey = 'refresh_token';
  static const String userIdKey = 'user_id';
  static const String customerIdKey = 'customer_id';

  // Pagination
  static const int defaultPageSize = 20;
  static const int defaultPageNumber = 1;

  // ==================== Auth Endpoints ====================

  /// Send OTP verification code
  /// POST /api/v1/Auth/send-verification-code
  static const String sendVerificationCode = '/$apiVersion/Auth/send-verification-code';

  /// Complete customer authentication (verify OTP + login/register)
  /// POST /api/v1/Auth/customer/complete-authentication
  static const String completeCustomerAuth = '/$apiVersion/Auth/customer/complete-authentication';

  /// Resend OTP code
  /// POST /api/v1/PhoneVerification/resend
  static const String resendOtp = '/$apiVersion/PhoneVerification/resend';

  /// Refresh access token
  /// POST /api/v1/Auth/refresh
  static const String refreshToken = '/$apiVersion/Auth/refresh';

  /// Logout
  /// POST /api/v1/Auth/logout
  static const String logout = '/$apiVersion/Auth/logout';

  // ==================== User Endpoints ====================

  /// Get user profile
  /// GET /api/v1/Users/profile
  static const String userProfile = '/$apiVersion/Users/profile';

  /// Update user profile
  /// PUT /api/v1/Users/profile
  static const String updateUserProfile = '/$apiVersion/Users/profile';

  // ==================== Category Endpoints ====================

  /// Get all categories
  /// GET /api/v1/Categories
  static const String categories = '/$apiVersion/Categories';

  /// Get popular categories
  /// GET /api/v1/Categories/popular
  static const String popularCategories = '/$apiVersion/Categories/popular';

  // ==================== Provider Endpoints ====================

  /// Search providers
  /// POST /api/v1/Providers/search
  static const String searchProviders = '/$apiVersion/Providers/search';

  /// Get provider by ID
  /// GET /api/v1/Providers/{id}
  static String providerById(String id) => '/$apiVersion/Providers/$id';

  /// Get provider services
  /// GET /api/v1/Providers/{id}/services
  static String providerServices(String id) => '/$apiVersion/Providers/$id/services';

  // ==================== Booking Endpoints ====================

  /// Get my bookings
  /// GET /api/v1/Bookings/my-bookings
  static const String myBookings = '/$apiVersion/Bookings/my-bookings';

  /// Create booking
  /// POST /api/v1/Bookings
  static const String createBooking = '/$apiVersion/Bookings';

  /// Get booking by ID
  /// GET /api/v1/Bookings/{id}
  static String bookingById(String id) => '/$apiVersion/Bookings/$id';

  /// Cancel booking
  /// POST /api/v1/Bookings/{id}/cancel
  static String cancelBooking(String id) => '/$apiVersion/Bookings/$id/cancel';

  /// Reschedule booking
  /// POST /api/v1/Bookings/{id}/reschedule
  static String rescheduleBooking(String id) => '/$apiVersion/Bookings/$id/reschedule';

  // ==================== Availability Endpoints ====================

  /// Check provider availability
  /// GET /api/v1/Availability/provider/{providerId}
  static String providerAvailability(String providerId) =>
      '/$apiVersion/Availability/provider/$providerId';

  /// Get available time slots
  /// GET /api/v1/Availability/slots
  static const String availableSlots = '/$apiVersion/Availability/slots';

  // ==================== Review Endpoints ====================

  /// Get provider reviews
  /// GET /api/v1/Reviews/provider/{providerId}
  static String providerReviews(String providerId) =>
      '/$apiVersion/Reviews/provider/$providerId';

  /// Create review
  /// POST /api/v1/Reviews
  static const String createReview = '/$apiVersion/Reviews';

  // ==================== Location Endpoints ====================

  /// Search locations
  /// GET /api/v1/Locations/search
  static const String searchLocations = '/$apiVersion/Locations/search';

  /// Get nearby providers
  /// GET /api/v1/Locations/nearby
  static const String nearbyProviders = '/$apiVersion/Locations/nearby';

  // ==================== Customer Endpoints ====================

  /// Get customer's recently visited providers
  /// GET /api/v1/Customers/{customerId}/recently-visited
  static String recentlyVisitedProviders(String customerId) =>
      '/$apiVersion/Customers/$customerId/recently-visited';

  /// Record provider visit
  /// POST /api/v1/Customers/{customerId}/recently-visited
  static String recordProviderVisit(String customerId) =>
      '/$apiVersion/Customers/$customerId/recently-visited';

  /// Get customer's favorite providers
  /// GET /api/v1/Customers/{customerId}/favorites
  static String customerFavorites(String customerId) =>
      '/$apiVersion/Customers/$customerId/favorites';
}
