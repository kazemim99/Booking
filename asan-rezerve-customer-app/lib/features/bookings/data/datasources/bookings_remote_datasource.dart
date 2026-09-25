import 'package:dio/dio.dart';
import '../../../../core/api/config/api_constants.dart';
import '../../../../core/utils/wall_clock.dart';

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

  /// One booking by id (`BookingDetailsResponse`). Used when a booking is not
  /// among the newest of either list; the server answers 403 unless the
  /// caller is the booking's customer, its salon or an admin.
  Future<Map<String, dynamic>> getBookingById(String bookingId) async {
    final response = await serviceCatalogDio.get(ApiConstants.bookingById(bookingId));
    final data = _unwrap(response.data);
    if (response.statusCode == 200 && data is Map<String, dynamic>) {
      return data;
    }
    throw DioException(
      requestOptions: response.requestOptions,
      response: response,
      type: DioExceptionType.badResponse,
      message: 'Failed to load booking',
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

  /// Moves the booking. The server closes this one (`Rescheduled`) and
  /// creates a new one in `Requested` for the salon to confirm; the new
  /// booking's id is returned when the answer names it, else null.
  Future<String?> rescheduleBooking({
    required String bookingId,
    required DateTime newStartTime,
    String? newStaffId,
  }) async {
    final response = await serviceCatalogDio.post(
      ApiConstants.rescheduleBooking(bookingId),
      data: {
        'newStartTime': wallClockIso(newStartTime),
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
    return newBookingIdFrom(response.data);
  }

  static final _guid = RegExp(
      r'[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}');

  /// The new booking's id in the reschedule answer: a `newBookingId` field
  /// (top level or inside `data`) when the server sends one, else the id the
  /// message names — today the endpoint answers only
  /// `{ message: "Booking rescheduled successfully. New booking ID: <guid>" }`.
  static String? newBookingIdFrom(Object? body) {
    final levels = [
      if (body is Map) body,
      if (body is Map && body['data'] is Map) body['data'] as Map,
    ];
    for (final level in levels) {
      final id = level['newBookingId'];
      if (id is String && id.trim().isNotEmpty) return id.trim();
    }
    for (final level in levels) {
      final message = level['message'];
      if (message is String) {
        final marker = message.indexOf('New booking ID');
        if (marker >= 0) {
          final match = _guid.firstMatch(message.substring(marker));
          if (match != null) return match.group(0);
        }
      }
    }
    return null;
  }
}
