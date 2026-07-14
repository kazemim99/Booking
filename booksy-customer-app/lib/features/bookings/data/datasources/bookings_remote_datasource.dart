import 'package:dio/dio.dart';
import '../../../../core/api/config/api_constants.dart';

/// Remote data source for the customer's bookings (list, cancel,
/// reschedule). Manual JSON handling — codegen is unavailable.
class BookingsRemoteDataSource {
  final Dio serviceCatalogDio;

  BookingsRemoteDataSource({required this.serviceCatalogDio});

  static dynamic _unwrap(dynamic body) {
    if (body is Map<String, dynamic> && body.containsKey('data')) {
      return body['data'];
    }
    return body;
  }

  Future<List<Map<String, dynamic>>> getMyBookings({
    required bool upcoming,
    int pageSize = 50,
  }) async {
    final nowUtc = DateTime.now().toUtc().toIso8601String();
    final response = await serviceCatalogDio.get(
      ApiConstants.myBookings,
      queryParameters: {
        if (upcoming) 'from': nowUtc else 'to': nowUtc,
        'pageNumber': 1,
        'pageSize': pageSize,
        'sort': 'StartTime',
        'sortDesc': !upcoming,
      },
    );

    if (response.statusCode == 200) {
      final data = _unwrap(response.data);
      final List<dynamic> items;
      if (data is Map<String, dynamic> && data['items'] is List) {
        items = data['items'] as List<dynamic>;
      } else if (data is List) {
        items = data;
      } else {
        items = const [];
      }
      return items.whereType<Map<String, dynamic>>().toList();
    }

    throw DioException(
      requestOptions: response.requestOptions,
      response: response,
      type: DioExceptionType.badResponse,
      message: 'Failed to load bookings',
    );
  }

  Future<void> cancelBooking({
    required String bookingId,
    required String reason,
    required String cancelledBy,
  }) async {
    final response = await serviceCatalogDio.post(
      ApiConstants.cancelBooking(bookingId),
      data: {'reason': reason, 'cancelledBy': cancelledBy},
    );
    if (response.statusCode != 200 && response.statusCode != 204) {
      throw DioException(
        requestOptions: response.requestOptions,
        response: response,
        type: DioExceptionType.badResponse,
        message: 'Failed to cancel booking',
      );
    }
  }

  Future<void> rescheduleBooking({
    required String bookingId,
    required DateTime newStartTime,
    String? newStaffId,
  }) async {
    final response = await serviceCatalogDio.post(
      ApiConstants.rescheduleBooking(bookingId),
      data: {
        'newStartTime': newStartTime.toUtc().toIso8601String(),
        if (newStaffId != null) 'newStaffId': newStaffId,
      },
    );
    if (response.statusCode != 200 && response.statusCode != 204) {
      throw DioException(
        requestOptions: response.requestOptions,
        response: response,
        type: DioExceptionType.badResponse,
        message: 'Failed to reschedule booking',
      );
    }
  }
}
