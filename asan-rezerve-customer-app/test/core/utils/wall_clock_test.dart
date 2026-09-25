import 'package:flutter_test/flutter_test.dart';

import 'package:asan_rezerve_customer_app/core/utils/wall_clock.dart';
import 'package:asan_rezerve_customer_app/features/bookings/data/booking_summary_json.dart';

/// A booking's time is the salon's wall clock: «۱۰:۳۰» is half past ten at the salon, whatever zone the phone is in
/// and whatever zone suffix the server happens to write (QA 2026-09-23: the API wrote "…T10:30:00Z", the app moved it
/// to Tehran time and showed 14:00 for a 10:30 booking).
void main() {
  group('parseWallClock', () {
    for (final raw in const [
      '2026-09-24T10:30:00', // zone-less, as the API sends it now
      '2026-09-24T10:30:00Z', // what production sent on 2026-09-23
      '2026-09-24T10:30:00+03:30', // the older offset shape
      '2026-09-24T10:30:00.000Z',
    ]) {
      test('reads $raw as half past ten, not moved to any zone', () {
        final t = parseWallClock(raw);
        expect([t.year, t.month, t.day, t.hour, t.minute], [2026, 9, 24, 10, 30]);
        expect(t.isUtc, isFalse, reason: 'a wall-clock time is a local clock value, not a UTC instant');
      });
    }

    test('round-trips through wallClockIso unchanged', () {
      final t = parseWallClock('2026-09-24T10:30:00Z');
      expect(wallClockIso(t), '2026-09-24T10:30:00');
    });

    test('tryParseWallClock is null for anything unparsable', () {
      expect(tryParseWallClock(null), isNull);
      expect(tryParseWallClock(''), isNull);
      expect(tryParseWallClock('not a date'), isNull);
    });
  });

  test('an appointment booked for 10:30 reads as 10:30 whatever suffix the API wrote', () {
    for (final start in const ['2026-09-24T10:30:00Z', '2026-09-24T10:30:00']) {
      final booking = BookingSummaryJson.fromListItem({
        'bookingId': 'b1',
        'providerId': 'p1',
        'providerName': 'سالن نهال',
        'serviceId': 's1',
        'serviceName': 'اصلاح سر با ماشین',
        'startTime': start,
        'durationMinutes': 30,
        'totalPrice': 250000,
        'currency': 'IRT',
        'status': 'Confirmed',
      }, now: DateTime(2026, 9, 23, 20));

      expect(booking.startTime.hour, 10, reason: start);
      expect(booking.startTime.minute, 30, reason: start);
    }
  });
}
