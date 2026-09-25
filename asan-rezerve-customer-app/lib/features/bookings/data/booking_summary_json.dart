import '../../../core/utils/person_name.dart';
import '../../../core/utils/wall_clock.dart';
import '../../reviews/domain/entities/review.dart';
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
      rescheduleBlockedReason: json['rescheduleBlockedReason'],
      review: json,
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
      rescheduleBlockedReason: json['rescheduleBlockedReason'],
      review: json,
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
    required Object? rescheduleBlockedReason,
    required Map<String, dynamic> review,
    required DateTime now,
  }) {
    // The salon's wall clock: the digits are the time, whatever zone the server wrote (QA 2026-09-23).
    final start = parseWallClock(startTime);
    final statusText = (status ?? '').toString();
    final actionable =
        _actionableStatuses.contains(statusText.toLowerCase()) &&
            start.isAfter(now);
    final staff = staffId?.toString();

    // Where the review stands, as the server says (additive fields). An older server says nothing, and then a
    // completed visit is offered as before.
    final reviewId = _text(review['reviewId']);
    final serverCanReview = review['canReview'];
    final canReview = serverCanReview is bool
        ? serverCanReview
        : statusText.toLowerCase() == 'completed' && reviewId == null;

    return BookingSummary(
      id: id.toString(),
      providerId: (providerId ?? '').toString(),
      providerName: providerName as String? ?? '',
      providerImageUrl: providerImageUrl,
      serviceId: (serviceId ?? '').toString(),
      serviceName: serviceName as String? ?? '',
      staffId:
          staff == null || staff.isEmpty || staff == _emptyGuid ? null : staff,
      // Optional and additive (production QA 2026-09-23); a placeholder or a phone is never kept as a name.
      staffName: personNameOrNull(staffName is String ? staffName : null),
      startTime: start,
      durationMinutes: (durationMinutes as num?)?.toInt() ?? 0,
      price: (price as num?)?.toDouble() ?? 0,
      currency: currency as String? ?? '',
      status: statusText,
      canCancel: actionable,
      canReschedule: actionable,
      canReview: canReview,
      reviewBlockedReason: _text(review['reviewBlockedReason']),
      reviewId: reviewId,
      reviewStatus: reviewId == null ? null : ReviewModerationStatus.parse(review['reviewStatus']),
      // Per salon since reviews-and-reschedule-round2; absent on an older server, which never offered an edit here.
      reviewEditable: reviewId != null && review['reviewEditable'] == true,
      reviewBookingId: reviewId == null ? null : _text(review['reviewBookingId']),
      cancellationReason: cancellationReason,
      // Optional and additive; blank is none.
      rescheduleBlockedReason: _text(rescheduleBlockedReason),
      // Additive (add-discounts-and-campaigns): absent on an older server, zero without a discount.
      discountAmount: (review['discountAmount'] as num?)?.toDouble() ?? 0,
      discountTitle: _text(review['discountTitle']),
    );
  }

  static String? _text(Object? value) =>
      value is String && value.trim().isNotEmpty ? value.trim() : null;
}
