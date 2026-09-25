import 'package:dio/dio.dart';
import '../../../../core/api/config/api_constants.dart';
import '../../../../core/utils/wall_clock.dart';

/// Remote data source for provider detail, availability, and booking
/// creation. Parses JSON manually (codegen is unavailable) and tolerates
/// both the raw and the {success, data} wrapped response shapes.
class BookingRemoteDataSource {
  final Dio serviceCatalogDio;

  BookingRemoteDataSource({required this.serviceCatalogDio});

  /// Unwraps { success, data, metadata } envelopes; returns the payload.
  static dynamic unwrap(dynamic body) {
    if (body is Map<String, dynamic> && body.containsKey('data')) {
      return body['data'];
    }
    return body;
  }

  Future<Map<String, dynamic>> getProviderDetail(String providerId) async {
    final response = await serviceCatalogDio.get(
      ApiConstants.providerById(providerId),
      queryParameters: {'includeServices': true, 'includeStaff': true},
    );
    final data = unwrap(response.data);
    if (response.statusCode == 200 && data is Map<String, dynamic>) {
      return data;
    }
    throw DioException(
      requestOptions: response.requestOptions,
      response: response,
      type: DioExceptionType.badResponse,
      message: 'Failed to load provider',
    );
  }

  /// [serviceIds] is sent as repeated `ServiceIds=` query parameters, which is
  /// how the backend binds the multi-service visit; slots then span the summed
  /// duration of the whole set instead of `ServiceId` alone.
  Future<Map<String, dynamic>> getAvailableSlots({
    required String providerId,
    required String serviceId,
    required DateTime date,
    String? staffId,
    List<String>? serviceIds,
  }) async {
    final response = await serviceCatalogDio.get(
      ApiConstants.availableSlots,
      queryParameters: {
        'ProviderId': providerId,
        'ServiceId': serviceId,
        'Date': date.toIso8601String(),
        if (staffId != null) 'StaffId': staffId,
        if (serviceIds != null && serviceIds.isNotEmpty)
          'ServiceIds': serviceIds,
      },
    );
    final data = unwrap(response.data);
    if (response.statusCode == 200 && data is Map<String, dynamic>) {
      return data;
    }
    throw DioException(
      requestOptions: response.requestOptions,
      response: response,
      type: DioExceptionType.badResponse,
      message: 'Failed to load slots',
    );
  }

  /// Returns the created booking id.
  ///
  /// `serviceId` is always sent because the backend still marks it required;
  /// `serviceIds` supersedes it when present, and the booking's duration and
  /// price become the sums over that set.
  Future<String> createBooking({
    required String providerId,
    required String serviceId,
    required String staffProviderId,
    required DateTime startTime,
    List<String>? serviceIds,
    String? promotionCode,
  }) async {
    final response = await serviceCatalogDio.post(
      ApiConstants.createBooking,
      data: {
        'providerId': providerId,
        'serviceId': serviceId,
        'staffProviderId': staffProviderId,
        'startTime': wallClockIso(startTime),
        if (serviceIds != null && serviceIds.isNotEmpty)
          'serviceIds': serviceIds,
        // Only a code the quote accepted; the server prices the visit again and never takes a price from here.
        if (promotionCode != null && promotionCode.isNotEmpty) 'promotionCode': promotionCode,
      },
    );
    if (response.statusCode == 200 || response.statusCode == 201) {
      final data = unwrap(response.data);
      if (data is Map<String, dynamic>) {
        return (data['id'] ?? data['bookingId'] ?? '').toString();
      }
      return '';
    }
    throw DioException(
      requestOptions: response.requestOptions,
      response: response,
      type: DioExceptionType.badResponse,
      message: 'Failed to create booking',
    );
  }

  /// A salon's automatic offers. Anonymous on the server; a failure is the caller's to swallow.
  Future<List<Map<String, dynamic>>> getOffers(String providerId) async {
    final response = await serviceCatalogDio.get(ApiConstants.providerOffers(providerId));
    final data = unwrap(response.data);
    return data is List ? data.whereType<Map>().map((m) => Map<String, dynamic>.from(m)).toList() : const [];
  }

  /// The server's price for the visit, with [promotionCode] evaluated when given.
  Future<Map<String, dynamic>> quote({
    required String providerId,
    required List<String> serviceIds,
    required DateTime startTime,
    String? promotionCode,
  }) async {
    final response = await serviceCatalogDio.post(
      ApiConstants.quoteBooking,
      data: {
        'providerId': providerId,
        'serviceIds': serviceIds,
        'startTime': wallClockIso(startTime),
        'promotionCode': (promotionCode?.trim().isEmpty ?? true) ? null : promotionCode!.trim(),
      },
    );
    final data = unwrap(response.data);
    return data is Map ? Map<String, dynamic>.from(data) : <String, dynamic>{};
  }
}
