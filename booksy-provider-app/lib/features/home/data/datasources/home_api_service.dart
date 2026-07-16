import 'package:dio/dio.dart';

import '../../../../core/api/config/api_constants.dart';
import '../../../onboarding/domain/entities/onboarding_data.dart'
    show BreakTime, ClockTime, DayHours, GalleryImageUpload;
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

  // ==================== clients ====================

  /// GET /v1/Providers/{id}/clients — the client book as raw maps
  /// (envelope: {data: {clients: [...]}}, verified live 2026-07-16).
  Future<List<Map<String, dynamic>>> getProviderClients(
    String providerId,
  ) async {
    final res = await _dio.get(ApiConstants.providerClients(providerId));
    final clients = unwrapMap(res.data)['clients'];
    if (clients is! List) return const [];
    return clients.whereType<Map<String, dynamic>>().toList();
  }

  // ==================== booking composer ====================

  /// GET /v1/Providers/{id}/staff — the provider's staff as raw maps.
  Future<List<Map<String, dynamic>>> getProviderStaff(
    String providerId,
  ) async {
    final res = await _dio.get(ApiConstants.providerStaff(providerId));
    return unwrapList(res.data);
  }

  /// GET /v1/Bookings/available-slots — available start times for the
  /// selection, normalized to local wall-clock time.
  Future<List<DateTime>> getAvailableSlots({
    required String providerId,
    required String serviceId,
    required DateTime date,
    String? staffId,
  }) async {
    final res = await _dio.get(
      ApiConstants.availableSlots,
      queryParameters: {
        'providerId': providerId,
        'serviceId': serviceId,
        'date': date.toIso8601String(),
        'staffId': ?staffId,
      },
    );
    final slots = unwrapMap(res.data)['availableSlots'];
    if (slots is! List) return const [];
    return slots
        .whereType<Map<String, dynamic>>()
        .map(bookingStart)
        .whereType<DateTime>()
        .toList();
  }

  /// POST /v1/Bookings — creates a booking (customer = the JWT caller).
  Future<void> createBooking({
    required String providerId,
    required String serviceId,
    required String staffProviderId,
    required DateTime startTime,
    String? customerNotes,
  }) =>
      _dio.post(
        ApiConstants.bookings,
        data: {
          'providerId': providerId,
          'serviceId': serviceId,
          'staffProviderId': staffProviderId,
          'startTime': startTime.toUtc().toIso8601String(),
          if (customerNotes != null && customerNotes.isNotEmpty)
            'customerNotes': customerNotes,
        },
      );

  // ==================== business profile ====================

  /// GET /v1/Providers/{id} — provider details as a raw map (enveloped).
  Future<Map<String, dynamic>> getProviderDetails(String providerId) async {
    final res = await _dio.get(ApiConstants.providerDetails(providerId));
    return unwrapMap(res.data);
  }

  /// PUT /v1/Providers/business — updates the business's public info
  /// (provider resolved server-side from the caller).
  Future<void> updateBusinessInfo({
    required String businessName,
    String? description,
  }) =>
      _dio.put(
        ApiConstants.providerBusiness,
        data: {
          'businessName': businessName,
          'description': description ?? '',
        },
      );

  // ==================== working hours ====================

  /// GET /v1/providers/{id}/business-hours — the weekly hours with breaks.
  Future<List<DayHours>> getBusinessHours(String providerId) async {
    final res =
        await _dio.get(ApiConstants.providerBusinessHours(providerId));
    return parseBusinessHours(unwrapMap(res.data));
  }

  /// PUT /v1/providers/{id}/business-hours — replaces the weekly hours
  /// (step-6 wire shape; breaks included so they are never silently erased).
  Future<void> updateBusinessHours(
    String providerId,
    List<DayHours> days,
  ) =>
      _dio.put(
        ApiConstants.providerBusinessHours(providerId),
        data: {
          'businessHours': days.map(dayHoursToJson).toList(),
        },
      );

  /// Parses the GET shape (times as "HH:mm" strings, verified live
  /// 2026-07-16) into the shared [DayHours] model.
  static List<DayHours> parseBusinessHours(Map<String, dynamic> data) {
    final list = data['businessHours'];
    if (list is! List) return const [];
    return list.whereType<Map<String, dynamic>>().map((d) {
      return DayHours(
        dayOfWeek: readInt(d, const ['dayOfWeek']),
        isOpen: d['isOpen'] == true,
        openTime: clockFromText(d['openTime']),
        closeTime: clockFromText(d['closeTime']),
        breaks: switch (d['breaks']) {
          final List breaks => breaks
              .whereType<Map<String, dynamic>>()
              .map((b) {
                final start = clockFromText(b['startTime'] ?? b['start']);
                final end = clockFromText(b['endTime'] ?? b['end']);
                return (start == null || end == null)
                    ? null
                    : BreakTime(start, end);
              })
              .whereType<BreakTime>()
              .toList(),
          _ => const <BreakTime>[],
        },
      );
    }).toList();
  }

  /// "HH:mm" → [ClockTime]; null when absent/unparsable.
  static ClockTime? clockFromText(dynamic value) {
    if (value is! String || !value.contains(':')) return null;
    final parts = value.split(':');
    final h = int.tryParse(parts[0]);
    final m = int.tryParse(parts[1]);
    if (h == null || m == null) return null;
    return ClockTime(h, m);
  }

  /// The PUT wire shape (identical to onboarding step-6 serialization).
  static Map<String, dynamic> dayHoursToJson(DayHours d) {
    Map<String, dynamic>? time(ClockTime? t) =>
        t == null ? null : {'hours': t.hours, 'minutes': t.minutes};
    return {
      'dayOfWeek': d.dayOfWeek,
      'isOpen': d.isOpen,
      'openTime': time(d.openTime),
      'closeTime': time(d.closeTime),
      'breaks': d.breaks
          .map((b) => {'start': time(b.start), 'end': time(b.end)})
          .toList(),
    };
  }

  // ==================== holidays ====================

  /// GET /v1/providers/{id}/holidays — raw holiday maps
  /// (shape: {holidays:[{id, date:"yyyy-MM-dd", reason, isRecurring}]}).
  Future<List<Map<String, dynamic>>> getHolidays(String providerId) async {
    final res = await _dio.get(ApiConstants.providerHolidays(providerId));
    final holidays = unwrapMap(res.data)['holidays'];
    if (holidays is! List) return const [];
    return holidays.whereType<Map<String, dynamic>>().toList();
  }

  /// POST /v1/providers/{id}/holidays — adds a day off.
  Future<void> addHoliday(
    String providerId, {
    required DateTime date,
    required String reason,
    bool isRecurring = false,
  }) =>
      _dio.post(
        ApiConstants.providerHolidays(providerId),
        data: {
          'date':
              '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}',
          'reason': reason,
          'isRecurring': isRecurring,
        },
      );

  /// DELETE /v1/providers/{id}/holidays/{holidayId}.
  Future<void> deleteHoliday(String providerId, String holidayId) =>
      _dio.delete('${ApiConstants.providerHolidays(providerId)}/$holidayId');

  // ==================== gallery ====================

  /// GET /v1/Providers/{id}/gallery — raw image maps.
  Future<List<Map<String, dynamic>>> getGallery(String providerId) async {
    final res = await _dio.get(ApiConstants.providerGallery(providerId));
    return unwrapList(res.data);
  }

  /// POST /v1/Providers/{id}/gallery — multipart upload under `files`
  /// (same idiom as the onboarding step-7 upload).
  Future<void> uploadGalleryImages(
    String providerId,
    List<GalleryImageUpload> images,
  ) async {
    final form = FormData();
    for (final img in images) {
      form.files.add(MapEntry(
        'files',
        MultipartFile.fromBytes(img.bytes, filename: img.name),
      ));
    }
    await _dio.post(ApiConstants.providerGallery(providerId), data: form);
  }

  /// PUT /v1/Providers/{id}/gallery/{imageId}/set-primary.
  Future<void> setPrimaryGalleryImage(String providerId, String imageId) =>
      _dio.put(
          '${ApiConstants.providerGallery(providerId)}/$imageId/set-primary');

  /// DELETE /v1/Providers/{id}/gallery/{imageId}.
  Future<void> deleteGalleryImage(String providerId, String imageId) =>
      _dio.delete('${ApiConstants.providerGallery(providerId)}/$imageId');

  // ==================== staff management ====================

  /// POST /v1/Providers/{id}/staff — adds a team member.
  Future<void> addStaff(
    String providerId, {
    required String firstName,
    String? lastName,
    String? phoneNumber,
    String? role,
  }) =>
      _dio.post(
        ApiConstants.providerStaff(providerId),
        data: {
          'firstName': firstName,
          'lastName': lastName ?? '',
          if (phoneNumber != null && phoneNumber.isNotEmpty)
            'phoneNumber': phoneNumber,
          // Backend defaults the role to "ServiceProvider" when omitted.
          if (role != null && role.isNotEmpty) 'role': role,
        },
      );

  /// PUT /v1/Providers/{id}/staff/{staffId} — updates a team member.
  Future<void> updateStaff(
    String providerId,
    String staffId, {
    required String firstName,
    String? lastName,
    String? phoneNumber,
    String? role,
  }) =>
      _dio.put(
        '${ApiConstants.providerStaff(providerId)}/$staffId',
        data: {
          'firstName': firstName,
          'lastName': lastName ?? '',
          if (phoneNumber != null && phoneNumber.isNotEmpty)
            'phoneNumber': phoneNumber,
          if (role != null && role.isNotEmpty) 'role': role,
        },
      );

  /// DELETE /v1/Providers/{id}/staff/{staffId} — removes a team member.
  Future<void> removeStaff(String providerId, String staffId) =>
      _dio.delete('${ApiConstants.providerStaff(providerId)}/$staffId');

  // ==================== booking quick actions ====================

  /// POST /v1/Bookings/{id}/confirm — provider approves a pending request.
  Future<void> confirmBooking(String id) =>
      _dio.post(ApiConstants.bookingConfirm(id), data: const {});

  /// POST /v1/Bookings/{id}/cancel — provider declines/cancels.
  /// [cancelledBy] is the current USER id: the backend binds it as a Guid
  /// (the docs' `"cancelledBy": "string"` is wrong — a non-Guid value fails
  /// body binding and 500s; verified live 2026-07-15).
  Future<void> cancelBooking(
    String id, {
    required String reason,
    required String cancelledBy,
  }) =>
      _dio.post(
        ApiConstants.bookingCancel(id),
        data: {'reason': reason, 'cancelledBy': cancelledBy},
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
