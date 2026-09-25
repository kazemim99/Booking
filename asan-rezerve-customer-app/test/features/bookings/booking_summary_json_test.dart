import 'package:asan_rezerve_customer_app/features/bookings/data/booking_summary_json.dart';
import 'package:asan_rezerve_customer_app/features/reviews/domain/entities/review.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:asan_rezerve_customer_app/features/bookings/domain/entities/booking_summary.dart';

/// Who does the work (QA recording 2026-09-23 #8): the booking APIs send the staff member's name as `staffName`
/// on both shapes — the `my-bookings` list item and `GET /Bookings/{id}`. It is optional: an API without it, or a
/// booking where nobody was named, parses as before.
void main() {
  final now = DateTime(2026, 9, 23, 10);

  Map<String, dynamic> listItem([Map<String, dynamic> extra = const {}]) => {
        'bookingId': 'b1',
        'providerId': 'p1',
        'providerName': 'سالن نهال',
        'serviceId': 's1',
        'serviceName': 'اصلاح سر با ماشین',
        'staffId': 'st1',
        'startTime': '2026-09-24T10:30:00',
        'durationMinutes': 30,
        'totalPrice': 150000,
        'currency': 'IRT',
        'status': 'Requested',
        ...extra,
      };

  Map<String, dynamic> details([Map<String, dynamic> extra = const {}]) => {
        'id': 'b1',
        'providerId': 'p1',
        'providerBusinessName': 'سالن نهال',
        'serviceId': 's1',
        'serviceName': 'اصلاح سر با ماشین',
        'staffProviderId': 'st1',
        'startTime': '2026-09-24T10:30:00',
        'durationMinutes': 30,
        'status': 'Confirmed',
        'paymentInfo': {'totalAmount': 150000, 'currency': 'IRT'},
        ...extra,
      };

  // QA 2026-09-24: the 24-hour reschedule rule worked, but the customer met it only at the very end. The server now
  // says up front why an active booking cannot be moved (Persian), on both shapes; absent means it can be.
  group('why a booking cannot be moved', () {
    const reason = 'تغییر زمان تا 24 ساعت پیش از نوبت ممکن است؛ برای تغییر با سالن تماس بگیرید.';

    test('a list item carries the reason the server sent', () {
      final booking = BookingSummaryJson.fromListItem(listItem({'rescheduleBlockedReason': reason}), now: now);
      expect(booking.rescheduleBlockedReason, reason);
      expect(booking.canReschedule, isTrue, reason: 'the button stays, disabled, so the reason has a place');
    });

    test('a booking read by id carries it too', () {
      final booking = BookingSummaryJson.fromDetails(details({'rescheduleBlockedReason': reason}), now: now);
      expect(booking.rescheduleBlockedReason, reason);
    });

    test('without the field the booking can be moved as before', () {
      expect(BookingSummaryJson.fromListItem(listItem(), now: now).rescheduleBlockedReason, isNull);
      expect(BookingSummaryJson.fromDetails(details(), now: now).rescheduleBlockedReason, isNull);
    });

    test('a blank reason is no reason', () {
      expect(BookingSummaryJson.fromListItem(listItem({'rescheduleBlockedReason': '  '}), now: now).rescheduleBlockedReason,
          isNull);
    });
  });

  group('a list item', () {
    test('carries the staff member\'s name', () {
      final booking = BookingSummaryJson.fromListItem(listItem({'staffName': 'مریم احمدی'}), now: now);
      expect(booking.staffName, 'مریم احمدی');
    });

    test('without the field has no staff name', () {
      expect(BookingSummaryJson.fromListItem(listItem(), now: now).staffName, isNull);
    });
  });

  group('a booking read by id', () {
    test('carries the staff member\'s name', () {
      final booking = BookingSummaryJson.fromDetails(details({'staffName': ' مریم احمدی '}), now: now);
      expect(booking.staffName, 'مریم احمدی');
    });

    test('without the field has no staff name', () {
      expect(BookingSummaryJson.fromDetails(details(), now: now).staffName, isNull);
    });
  });

  test('an empty name, a placeholder or a phone number is no name', () {
    for (final raw in <Object?>[null, '', '   ', 'ارائه‌دهنده 9123135143', '09123135143', 42]) {
      expect(
        BookingSummaryJson.fromListItem(listItem({'staffName': raw}), now: now).staffName,
        isNull,
        reason: 'staffName: $raw',
      );
    }
  });

  // openspec/changes/_inline/customer-reviews-and-nahal-seed: the server says where a visit's review stands, so the
  // app neither offers «ثبت نظر» on status alone nor forgets a review it saved once the page closes.
  group('where the review stands', () {
    const waiting = 'پس از اینکه سالن این نوبت را «انجام‌شده» ثبت کند، می‌توانید برایش نظر بنویسید.';

    test('a completed visit the server says can be reviewed', () {
      final booking = BookingSummaryJson.fromListItem(listItem({'status': 'Completed', 'canReview': true}), now: now);
      expect(booking.canReview, isTrue);
      expect(booking.hasReview, isFalse);
    });

    test('a reviewed visit carries its review and is not offered again', () {
      final booking = BookingSummaryJson.fromListItem(
        listItem({'status': 'Completed', 'canReview': false, 'reviewId': 'r1', 'reviewStatus': 'Published'}),
        now: now,
      );
      expect(booking.canReview, isFalse);
      expect(booking.reviewId, 'r1');
      expect(booking.reviewStatus, ReviewModerationStatus.published);
      expect(booking.hasReview, isTrue);
    });

    test('a visit waiting for the salon says why, on both shapes', () {
      final item = BookingSummaryJson.fromListItem(
          listItem({'status': 'Confirmed', 'canReview': false, 'reviewBlockedReason': waiting}), now: now);
      final byId = BookingSummaryJson.fromDetails(details({'canReview': false, 'reviewBlockedReason': waiting}), now: now);
      expect(item.reviewBlockedReason, waiting);
      expect(byId.reviewBlockedReason, waiting);
      expect(item.canReview, isFalse);
    });

    test('the server has the last word over the status', () {
      final booking = BookingSummaryJson.fromDetails(details({'status': 'Completed', 'canReview': false}), now: now);
      expect(booking.canReview, isFalse, reason: 'on the booking page canReview is only for the person it is for');
    });

    // reviews-and-reschedule-round2 item 8: the review state is per salon, and says whether it can be edited.
    test('reviewEditable is read on both shapes; the edit is offered only with a review', () {
      final item = BookingSummaryJson.fromListItem(
          listItem({'status': 'Completed', 'canReview': false, 'reviewId': 'r1', 'reviewStatus': 'Published',
              'reviewEditable': true}),
          now: now);
      final byId = BookingSummaryJson.fromDetails(
          details({'canReview': false, 'reviewId': 'r1', 'reviewStatus': 'Pending', 'reviewEditable': true}),
          now: now);
      expect(item.reviewEditable, isTrue);
      expect(item.canEditReview, isTrue);
      expect(byId.canEditReview, isTrue);

      final locked = BookingSummaryJson.fromListItem(
          listItem({'reviewId': 'r1', 'reviewStatus': 'Published', 'reviewEditable': false}), now: now);
      expect(locked.canEditReview, isFalse);
      final noReview = BookingSummaryJson.fromListItem(listItem({'reviewEditable': true}), now: now);
      expect(noReview.canEditReview, isFalse);
      expect(BookingSummaryJson.fromListItem(listItem({'reviewId': 'r1'}), now: now).reviewEditable, isFalse,
          reason: 'an older server never said it could be edited here');
    });

    test('a review written from another visit to the salon is told apart when the server names the visit', () {
      BookingSummary read(Map<String, dynamic> extra) =>
          BookingSummaryJson.fromListItem(listItem({'reviewId': 'r1', 'reviewStatus': 'Published', ...extra}), now: now);
      final id = read(const {}).id;
      expect(read({'reviewBookingId': 'another'}).reviewFromOtherVisit, isTrue);
      expect(read({'reviewBookingId': id}).reviewFromOtherVisit, isFalse);
      expect(read(const {}).reviewFromOtherVisit, isFalse);
    });

    test('an older server without the fields: a completed visit is offered, as before', () {
      expect(BookingSummaryJson.fromListItem(listItem({'status': 'Completed'}), now: now).canReview, isTrue);
      expect(BookingSummaryJson.fromListItem(listItem(), now: now).canReview, isFalse);
      expect(BookingSummaryJson.fromListItem(listItem(), now: now).reviewStatus, isNull);
    });
  });
}
