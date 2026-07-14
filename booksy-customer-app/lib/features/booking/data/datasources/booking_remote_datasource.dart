import 'package:dio/dio.dart';
import '../../../../core/api/config/api_constants.dart';

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

  Future<Map<String, dynamic>> getAvailableSlots({
    required String providerId,
    required String serviceId,
    required DateTime date,
    String? staffId,
  }) async {
    final response = await serviceCatalogDio.get(
      ApiConstants.availableSlots,
      queryParameters: {
        'ProviderId': providerId,
        'ServiceId': serviceId,
        'Date': date.toIso8601String(),
        if (staffId != null) 'StaffId': staffId,
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
  Future<String> createBooking({
    required String providerId,
    required String serviceId,
    required String staffProviderId,
    required DateTime startTime,
  }) async {
    final response = await serviceCatalogDio.post(
      ApiConstants.createBooking,
      data: {
        'providerId': providerId,
        'serviceId': serviceId,
        'staffProviderId': staffProviderId,
        'startTime': startTime.toUtc().toIso8601String(),
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
}
