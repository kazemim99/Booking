import 'package:booksy_customer_app/features/booking/data/repositories/booking_repository_impl.dart';
import 'package:booksy_customer_app/features/booking/domain/business_days.dart';
import 'package:booksy_customer_app/features/booking/domain/entities/booking_entities.dart';
import 'package:flutter_test/flutter_test.dart';

/// The day strip greys out the weekdays a salon is shut (UX review 2026-09-23, #4). The salon's hours name each day
/// in Persian, and the same day may be spelled with the zero-width non-joiner or with a space.
void main() {
  BusinessHour day(String name, {bool closed = true}) => BusinessHour(dayOfWeek: name, isClosed: closed);

  test('closed days map to DateTime weekdays; open days do not', () {
    final closed = BusinessDays.closedWeekdays([
      day('جمعه'),
      day('شنبه', closed: false),
      day('پنجشنبه'),
    ]);

    expect(closed, {DateTime.friday, DateTime.thursday});
  });

  test('a day spelled with a joiner, a space or none is the same day', () {
    expect(BusinessDays.weekdayOf('سه‌شنبه'), DateTime.tuesday);
    expect(BusinessDays.weekdayOf('سه شنبه'), DateTime.tuesday);
    expect(BusinessDays.weekdayOf('سهشنبه'), DateTime.tuesday);
    expect(BusinessDays.weekdayOf('یک‌شنبه'), DateTime.sunday);
    expect(BusinessDays.weekdayOf('پنج شنبه'), DateTime.thursday);
    // Arabic yeh, as some keyboards type it.
    expect(BusinessDays.weekdayOf('يکشنبه'), DateTime.sunday);
  });

  test('an unreadable day is never treated as closed', () {
    expect(BusinessDays.closedWeekdays([day('6'), day('')]), isEmpty);
  });

  // The parser turns the server's DayOfWeek number (0 = Sunday … 6 = Saturday) into the name the UI shows. Each
  // index is closed on its own, so a shifted or swapped mapping (Saturday read as Sunday, say) greys out the wrong
  // day instead of hiding behind a set that holds all seven.
  const serverDayToWeekday = {
    0: DateTime.sunday,
    1: DateTime.monday,
    2: DateTime.tuesday,
    3: DateTime.wednesday,
    4: DateTime.thursday,
    5: DateTime.friday,
    6: DateTime.saturday,
  };
  for (final MapEntry(key: serverDay, value: weekday) in serverDayToWeekday.entries) {
    test('the server closing day $serverDay closes DateTime weekday $weekday and no other', () {
      final hours = BookingRepositoryImpl.parseBusinessHours([
        for (var i = 0; i < 7; i++) {'dayOfWeek': i, 'isOpen': i != serverDay},
      ]);

      expect(BusinessDays.closedWeekdays(hours), {weekday});
    });
  }
}
