import 'package:dio/dio.dart';

import '../../../../core/api/config/api_constants.dart';
import '../../domain/entities/home_booking.dart';

/// Raw API access for the Home snapshot (manual JSON — no codegen, see
/// CLAUDE.md). Parsing helpers are pure statics so they unit-test without Dio.
class HomeApiService {
  final Dio _dio; // authenticated

  HomeApiService(this._dio);

  /// GET /v1/Bookings/provider/{id}?from&to — bookings in the window, as raw
  /// maps. Tolerates both a bare JSON array and PagedResult-style envelopes.
  Future<List<Map<String, dynamic>>> getProviderBookings(
    String providerId, {
    required DateTime from,
    required DateTime to,
  }) async {
    final res = await _dio.get(
      ApiConstants.providerBookings(providerId),
      queryParameters: {
        'from': from.toIso8601String(),
        'to': to.toIso8601String(),
      },
    );
    return unwrapList(res.data);
  }

  /// GET /v1/Bookings/statistics?providerId[&startDate&endDate] — the stats
  /// object, unwrapped from the `{success, data: {...}}` envelope (verified
  /// live 2026-07-15). Returns {} when the response has no usable body.
  Future<Map<String, dynamic>> getBookingStatistics(
    String providerId, {
    DateTime? startDate,
    DateTime? endDate,
  }) async {
    final res = await _dio.get(
      ApiConstants.bookingStatistics,
      queryParameters: {
        'providerId': providerId,
        if (startDate != null) 'startDate': startDate.toIso8601String(),
        if (endDate != null) 'endDate': endDate.toIso8601String(),
      },
    );
    return unwrapMap(res.data);
  }

  /// GET /v1/Services/provider/{id} — the provider's services as raw maps
  /// (used to resolve service names for booking rows; shape: data.items[]).
  Future<List<Map<String, dynamic>>> getProviderServices(
    String providerId,
  ) async {
    final res = await _dio.get(ApiConstants.providerServices(providerId));
    return unwrapList(res.data);
  }

  // ==================== booking quick actions ====================

  /// POST /v1/Bookings/{id}/confirm — provider approves a pending request.
  Future<void> confirmBooking(String id) =>
      _dio.post(ApiConstants.bookingConfirm(id), data: const {});

  /// POST /v1/Bookings/{id}/cancel — provider declines/cancels.
  Future<void> cancelBooking(String id, {required String reason}) =>
      _dio.post(
        ApiConstants.bookingCancel(id),
        data: {'reason': reason, 'cancelledBy': 'Provider'},
      );

  /// POST /v1/Bookings/{id}/complete.
  Future<void> completeBooking(String id) =>
      _dio.post(ApiConstants.bookingComplete(id), data: const {});

  /// POST /v1/Bookings/{id}/no-show.
  Future<void> markNoShow(String id) =>
      _dio.post(ApiConstants.bookingNoShow(id), data: const {});

  // ==================== pure parsing helpers ====================

  /// Extracts a list of maps from a bare array, a paged envelope
  /// (`items`/`data`/`results`), or the API's nested envelope
  /// `{success, data: {items: [...]}}` (verified live 2026-07-15).
  static List<Map<String, dynamic>> unwrapList(dynamic data) {
    dynamic list = data;
    if (data is Map<String, dynamic>) {
      list = data['items'] ?? data['data'] ?? data['results'];
      if (list is Map<String, dynamic>) {
        // Envelope-in-envelope: {data: {items: [...]}}.
        list = list['items'] ?? list['data'] ?? list['results'];
      }
    }
    if (list is List) {
      return list.whereType<Map<String, dynamic>>().toList();
    }
    return const [];
  }

  /// Extracts an object payload from the API envelope `{success, data: {...}}`
  /// or returns the map itself when unwrapped.
  static Map<String, dynamic> unwrapMap(dynamic data) {
    if (data is Map<String, dynamic>) {
      final inner = data['data'];
      if (inner is Map<String, dynamic>) return inner;
      return data;
    }
    return const {};
  }

  /// Backend error code from a wrapped error response
  /// (`{error: {code: ...}}`); null when absent.
  static String? errorCode(dynamic data) {
    if (data is Map<String, dynamic>) {
      final error = data['error'];
      if (error is Map<String, dynamic>) {
        final code = error['code'];
        if (code is String && code.isNotEmpty) return code;
      }
    }
    return null;
  }

  /// Booking status, canonicalized to lower-case (`''` when absent).
  static String bookingStatus(Map<String, dynamic> booking) {
    final s = booking['status'] ?? booking['bookingStatus'];
    return s is String ? s.toLowerCase() : '';
  }

  /// Booking start time, from the first recognized key; null when unparsable.
  /// Normalized to LOCAL time: the API emits offset timestamps (e.g.
  /// `+03:30`, verified live) which Dart parses as UTC — display and
  /// same-day comparisons need wall-clock time.
  static DateTime? bookingStart(Map<String, dynamic> booking) {
    for (final key in const [
      'startTime',
      'scheduledStartTime',
      'bookingDate',
      'start',
      'scheduledAt',
    ]) {
      final v = booking[key];
      if (v is String) {
        final parsed = DateTime.tryParse(v);
        if (parsed != null) return parsed.toLocal();
      }
    }
    return null;
  }

  /// First non-empty string found under [keys]; [fallback] otherwise.
  static String readString(
    Map<String, dynamic> map,
    List<String> keys, {
    String fallback = '',
  }) {
    for (final key in keys) {
      final v = map[key];
      if (v is String && v.trim().isNotEmpty) return v.trim();
    }
    return fallback;
  }

  /// Maps a raw booking to the Home row model (tolerant of shape variations).
  static HomeBooking toHomeBooking(Map<String, dynamic> booking) {
    final status = switch (bookingStatus(booking)) {
      'pending' || 'requested' => HomeBookingStatus.pending,
      'completed' => HomeBookingStatus.completed,
      'noshow' || 'no-show' || 'no_show' => HomeBookingStatus.noShow,
      'cancelled' || 'canceled' => HomeBookingStatus.cancelled,
      _ => HomeBookingStatus.confirmed, // confirmed/in-progress/unknown
    };
    return HomeBooking(
      id: readString(booking, const ['id', 'bookingId']),
      start: bookingStart(booking),
      clientName: readString(
        booking,
        const ['customerName', 'clientName', 'customerFullName'],
      ),
      clientPhone: readString(
        booking,
        const ['customerPhone', 'clientPhone', 'customerPhoneNumber'],
      ),
      serviceName: readString(
        booking,
        const ['serviceName', 'serviceTitle', 'service'],
      ),
      status: status,
    );
  }

  /// First integer found under [keys] in [map]; [fallback] otherwise.
  static int readInt(
    Map<String, dynamic> map,
    List<String> keys, {
    int fallback = 0,
  }) {
    for (final key in keys) {
      final v = map[key];
      if (v is int) return v;
      if (v is num) return v.toInt();
      if (v is String) {
        final parsed = int.tryParse(v);
        if (parsed != null) return parsed;
      }
    }
    return fallback;
  }
}
