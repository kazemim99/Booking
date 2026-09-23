import '../../../core/utils/person_name.dart';
import '../../../core/utils/wall_clock.dart';
import '../domain/entities/booking_summary.dart';

/// Maps the two booking shapes the API returns to [BookingSummary]. Manual
/// JSON handling — codegen is unavailable.
///
/// Cancel/reschedule eligibility is derived from what the API allows: an
/// active status and a start still ahead of [now].
class BookingSummaryJson {
  BookingSummaryJson._();

  static const _actionableStatuses = {'pending', 'requested', 'confirmed'};
  static const _emptyGuid = '00000000-0000-0000-0000-000000000000';

  /// A `my-bookings` item (`CustomerBookingDto`).
  static BookingSummary fromListItem(
    Map<String, dynamic> json, {
    required DateTime now,
  }) {
    return _build(
      id: json['bookingId'] ?? json['id'],
      providerId: json['providerId'],
      providerName: json['providerName'],
      providerImageUrl: json['providerImageUrl'] as String?,
      serviceId: json['serviceId'],
      serviceName: json['serviceName'],
      staffId: json['staffId'],
      staffName: json['staffName'],
      startTime: json['startTime'] as String,
      durationMinutes: json['durationMinutes'],
      price: json['totalPrice'] ?? json['totalAmount'],
      currency: json['currency'],
      status: json['status'],
      cancellationReason: json['cancellationReason'] as String?,
      now: now,
    );
  }

  /// `GET /Bookings/{id}` (`BookingDetailsResponse`): `id`,
  /// `providerBusinessName`, `staffProviderId` (all zeros when nobody was
  /// named) and the price inside `paymentInfo`.
  static BookingSummary fromDetails(
    Map<String, dynamic> json, {
    required DateTime now,
  }) {
    final payment = json['paymentInfo'] is Map<String, dynamic>
        ? json['paymentInfo'] as Map<String, dynamic>
        : const <String, dynamic>{};
    return _build(
      id: json['id'] ?? json['bookingId'],
      providerId: json['providerId'],
      providerName: json['providerBusinessName'] ?? json['providerName'],
      providerImageUrl: json['providerImageUrl'] as String?,
      serviceId: json['serviceId'],
      serviceName: json['serviceName'],
      staffId: json['staffProviderId'] ?? json['staffId'],
      staffName: json['staffName'],
      startTime: json['startTime'] as String,
      durationMinutes: json['durationMinutes'],
      price: payment['totalAmount'] ?? json['totalPrice'],
      currency: payment['currency'] ?? json['currency'],
      status: json['status'],
      cancellationReason: json['cancellationReason'] as String?,
      now: now,
    );
  }

  static BookingSummary _build({
    required Object? id,
    required Object? providerId,
    required Object? providerName,
    required String? providerImageUrl,
    required Object? serviceId,
    required Object? serviceName,
    required Object? staffId,
    required Object? staffName,
    required String startTime,
    required Object? durationMinutes,
    required Object? price,
    required Object? currency,
    required Object? status,
    required String? cancellationReason,
    required DateTime now,
  }) {
    // The salon's wall clock: the digits are the time, whatever zone the server wrote (QA 2026-09-23).
    final start = parseWallClock(startTime);
    final statusText = (status ?? '').toString();
    final actionable =
        _actionableStatuses.contains(statusText.toLowerCase()) &&
            start.isAfter(now);
    final staff = staffId?.toString();

    return BookingSummary(
      id: id.toString(),
      providerId: (providerId ?? '').toString(),
      providerName: providerName as String? ?? '',
      providerImageUrl: providerImageUrl,
      serviceId: (serviceId ?? '').toString(),
      serviceName: serviceName as String? ?? '',
      staffId:
          staff == null || staff.isEmpty || staff == _emptyGuid ? null : staff,
      // Optional on the wire: an API without it, or a booking with nobody named, has no staff name.
      staffName: staffName is String ? realFullNameOrNull(staffName) : null,
      startTime: start,
      durationMinutes: (durationMinutes as num?)?.toInt() ?? 0,
      price: (price as num?)?.toDouble() ?? 0,
      currency: currency as String? ?? '',
      status: statusText,
      canCancel: actionable,
      canReschedule: actionable,
      canReview: statusText.toLowerCase() == 'completed',
      cancellationReason: cancellationReason,
    );
  }
}
