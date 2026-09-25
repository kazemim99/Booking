import 'package:dio/dio.dart';
import '../../../../core/api/config/api_constants.dart';

/// Remote data source for checkout. Parses JSON manually (codegen is unavailable in this project) and tolerates
/// both raw and `{success, data}` wrapped response shapes, matching the booking data source.
class CheckoutRemoteDataSource {
  final Dio serviceCatalogDio;

  CheckoutRemoteDataSource({required this.serviceCatalogDio});

  /// Unwraps `{ success, data, metadata }` envelopes; returns the payload.
  static dynamic unwrap(dynamic body) {
    if (body is Map<String, dynamic> && body.containsKey('data')) {
      return body['data'];
    }
    return body;
  }

  static Map<String, dynamic> _asMap(dynamic value) {
    final unwrapped = unwrap(value);
    if (unwrapped is Map<String, dynamic>) return unwrapped;
    if (unwrapped is Map) return Map<String, dynamic>.from(unwrapped);
    throw const FormatException('Expected a JSON object in the response body');
  }

  Future<Map<String, dynamic>> getBooking(String bookingId) async {
    final response = await serviceCatalogDio.get(ApiConstants.bookingById(bookingId));
    return _asMap(response.data);
  }

  /// Creates a gateway payment. The idempotency key travels as a header so the server's reservation dedupes a
  /// retried create instead of charging twice.
  Future<Map<String, dynamic>> createZarinPalPayment({
    required String bookingId,
    required String providerId,
    required double amount,
    required String idempotencyKey,
    String? description,
    String? mobile,
    String? email,
  }) async {
    final response = await serviceCatalogDio.post(
      ApiConstants.createZarinPalPayment,
      data: {
        'bookingId': bookingId,
        'providerId': providerId,
        'amount': amount,
        if (description != null) 'description': description,
        if (mobile != null) 'mobile': mobile,
        if (email != null) 'email': email,
      },
      options: Options(headers: {'Idempotency-Key': idempotencyKey}),
    );
    return _asMap(response.data);
  }

  /// Verifies by authority. Server-authoritative and idempotent; `status` merely reports the gateway's claim.
  Future<Map<String, dynamic>> verifyZarinPalPayment({
    required String authority,
    String status = 'OK',
    String? idempotencyKey,
  }) async {
    final response = await serviceCatalogDio.post(
      ApiConstants.verifyZarinPalPayment,
      data: {'authority': authority, 'status': status},
      options: idempotencyKey == null
          ? null
          : Options(headers: {'Idempotency-Key': idempotencyKey}),
    );
    return _asMap(response.data);
  }

  Future<Map<String, dynamic>> getPayment(String paymentId) async {
    final response = await serviceCatalogDio.get(ApiConstants.paymentById(paymentId));
    return _asMap(response.data);
  }
}
