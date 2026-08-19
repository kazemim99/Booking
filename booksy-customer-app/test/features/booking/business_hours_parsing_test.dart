import 'package:booksy_customer_app/features/booking/data/repositories/booking_repository_impl.dart';
import 'package:flutter_test/flutter_test.dart';

/// Regression cover for the provider profile's working-hours block.
///
/// It rendered as a column of bare numbers against empty dashes — `6 –`, `2 –`, `4 –` — because the parser
/// disagreed with the server's payload on three points at once:
///
///   * `dayOfWeek` arrives as an integer, and `.toString()` printed `"6"` rather than a weekday name;
///   * the times arrive split as `openTimeHours` / `openTimeMinutes`, so reading `openTime` was always null
///     and every row showed `" – "`;
///   * the flag is `isOpen`, not `isClosed`, so it defaulted to false and a closed day was drawn as open.
///
/// The payload below is copied from a real `GET /api/v1/Providers/{id}` response, so these tests fail if the
/// wire format drifts again rather than only if someone edits the parser.
void main() {
  /// One day exactly as the server sends it.
  Map<String, dynamic> day({
    required int dayOfWeek,
    bool isOpen = true,
    int? openH = 9,
    int? openM = 0,
    int? closeH = 18,
    int? closeM = 30,
  }) =>
      {
        'dayOfWeek': dayOfWeek,
        'isOpen': isOpen,
        'openTimeHours': openH,
        'openTimeMinutes': openM,
        'closeTimeHours': closeH,
        'closeTimeMinutes': closeM,
        'breaks': const [],
      };

  group('parseBusinessHours', () {
    test('renders weekday names, never raw day numbers', () {
      final hours = BookingRepositoryImpl.parseBusinessHours([
        day(dayOfWeek: 6),
        day(dayOfWeek: 0),
        day(dayOfWeek: 3),
      ]);

      expect(hours.map((h) => h.dayOfWeek), ['شنبه', 'یکشنبه', 'چهارشنبه']);
      for (final h in hours) {
        expect(
          RegExp(r'^\d+$').hasMatch(h.dayOfWeek),
          isFalse,
          reason: 'a bare day number reached the UI — this was the original defect',
        );
      }
    });

    test('composes times from the split hour and minute fields', () {
      final hours = BookingRepositoryImpl.parseBusinessHours([
        day(dayOfWeek: 6, openH: 9, openM: 0, closeH: 18, closeM: 30),
      ]);

      expect(hours.single.openTime, '09:00');
      expect(hours.single.closeTime, '18:30');
    });

    test('pads single-digit hours and minutes', () {
      final hours = BookingRepositoryImpl.parseBusinessHours([
        day(dayOfWeek: 1, openH: 6, openM: 5, closeH: 9, closeM: 7),
      ]);

      expect(hours.single.openTime, '06:05');
      expect(hours.single.closeTime, '09:07');
    });

    test('reads isOpen, so a closed day is reported closed', () {
      final hours = BookingRepositoryImpl.parseBusinessHours([
        day(dayOfWeek: 5, isOpen: false),
      ]);

      expect(
        hours.single.isClosed,
        isTrue,
        reason: 'the payload says isOpen:false; the old parser looked for isClosed and defaulted to open',
      );
    });

    test('orders days by the Iranian week, which starts on Saturday', () {
      // Deliberately shuffled, the way the server returns them.
      final hours = BookingRepositoryImpl.parseBusinessHours([
        day(dayOfWeek: 2),
        day(dayOfWeek: 6),
        day(dayOfWeek: 4),
        day(dayOfWeek: 0),
        day(dayOfWeek: 5),
        day(dayOfWeek: 1),
        day(dayOfWeek: 3),
      ]);

      expect(hours.map((h) => h.dayOfWeek), [
        'شنبه',
        'یکشنبه',
        'دوشنبه',
        'سه‌شنبه',
        'چهارشنبه',
        'پنجشنبه',
        'جمعه',
      ]);
    });

    test('a missing time becomes null rather than an empty dash', () {
      final hours = BookingRepositoryImpl.parseBusinessHours([
        day(dayOfWeek: 6, openH: null, closeH: null),
      ]);

      expect(hours.single.openTime, isNull);
      expect(hours.single.closeTime, isNull);
    });

    test('still honours a legacy isClosed payload', () {
      final hours = BookingRepositoryImpl.parseBusinessHours([
        {'dayOfWeek': 5, 'isClosed': true},
      ]);

      expect(hours.single.isClosed, isTrue);
    });

    test('tolerates absent, empty and malformed input', () {
      expect(BookingRepositoryImpl.parseBusinessHours(null), isEmpty);
      expect(BookingRepositoryImpl.parseBusinessHours(const []), isEmpty);
      expect(BookingRepositoryImpl.parseBusinessHours('not a list'), isEmpty);
    });

    test('an out-of-range day sorts last instead of throwing', () {
      final hours = BookingRepositoryImpl.parseBusinessHours([
        day(dayOfWeek: 99),
        day(dayOfWeek: 6),
      ]);

      expect(hours.first.dayOfWeek, 'شنبه');
      expect(hours.length, 2);
    });
  });
}
