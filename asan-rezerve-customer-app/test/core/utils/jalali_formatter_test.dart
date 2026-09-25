import 'package:booksy_customer_app/core/utils/jalali_formatter.dart';
import 'package:flutter_test/flutter_test.dart';

/// customer-app-ux-review-fixes A.5: weekday names are the single-word spellings the rest of the app
/// uses (working hours, business hours). shamsi_date spells some of them as two words («یک شنبه»,
/// «چهار شنبه»), so the day strip and the booking recap read differently from the salon's hours.
void main() {
  // 2026-09-19 is a Saturday, the first day of the Iranian week.
  final saturday = DateTime(2026, 9, 19, 10);
  final week = [for (var i = 0; i < 7; i++) saturday.add(Duration(days: i))];

  const singleWord = [
    'شنبه',
    'یکشنبه',
    'دوشنبه',
    'سه‌شنبه', // with a zero-width non-joiner, as Persian writes it
    'چهارشنبه',
    'پنجشنبه',
    'جمعه',
  ];

  test('weekday names are single words, Saturday to Friday', () {
    expect(week.map(JalaliFormatter.weekday).toList(), singleWord);
  });

  test('no weekday name carries a space', () {
    for (final day in week) {
      expect(JalaliFormatter.weekday(day), isNot(contains(' ')));
    }
  });

  test('the full date leads with the same single-word weekday', () {
    for (var i = 0; i < week.length; i++) {
      final formatted = JalaliFormatter.formatDate(week[i]);
      expect(formatted, startsWith('${singleWord[i]} '));
      expect(formatted.split(' '), hasLength(3),
          reason: 'weekday, day, month — «$formatted»');
    }
  });

  test('the date-time form uses it too', () {
    final sunday = week[1];
    expect(JalaliFormatter.formatDateTime(sunday), startsWith('یکشنبه '));
  });
}
