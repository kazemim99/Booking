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

  test('every name the repository produces is understood', () {
    // The parser turns the server's DayOfWeek number (0 = Sunday … 6 = Saturday) into the name the UI shows.
    final hours = BookingRepositoryImpl.parseBusinessHours([
      for (var i = 0; i < 7; i++) {'dayOfWeek': i, 'isOpen': false},
    ]);

    expect(BusinessDays.closedWeekdays(hours), {1, 2, 3, 4, 5, 6, 7});
  });
}
