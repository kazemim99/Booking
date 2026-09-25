import 'package:asan_rezerve_customer_app/core/utils/jalali_formatter.dart';
import 'package:asan_rezerve_customer_app/core/utils/relative_time.dart';
import 'package:flutter_test/flutter_test.dart';

/// How long ago a notification arrived (UX review 2026-09-23, G.2: inbox rows showed no time at all). "Now"
/// is always passed in, so none of this depends on the clock the tests run under.
void main() {
  final now = DateTime(2026, 9, 23, 14, 30);

  String ago(Duration d) => relativeTime(now.subtract(d), now: now);

  test('under a minute is "just now"', () {
    expect(ago(Duration.zero), 'همین حالا');
    expect(ago(const Duration(seconds: 59)), 'همین حالا');
  });

  test('a time a little ahead of this device (clock skew) is also "just now"', () {
    expect(relativeTime(now.add(const Duration(minutes: 2)), now: now), 'همین حالا');
  });

  test('minutes, in Persian digits', () {
    expect(ago(const Duration(minutes: 1)), '۱ دقیقه پیش');
    expect(ago(const Duration(minutes: 5)), '۵ دقیقه پیش');
    expect(ago(const Duration(minutes: 59, seconds: 59)), '۵۹ دقیقه پیش');
  });

  test('hours, while it is still the same day', () {
    expect(ago(const Duration(hours: 1)), '۱ ساعت پیش');
    expect(ago(const Duration(hours: 2, minutes: 40)), '۲ ساعت پیش');
    expect(relativeTime(DateTime(2026, 9, 23, 0, 5), now: now), '۱۴ ساعت پیش');
  });

  test('past the first hour, the calendar day before is "yesterday", however few hours ago it was', () {
    expect(relativeTime(DateTime(2026, 9, 22, 22, 50), now: DateTime(2026, 9, 23, 0, 40)), 'دیروز');
    expect(relativeTime(DateTime(2026, 9, 22, 0, 1), now: now), 'دیروز');
  });

  test('anything older is a short Jalali date', () {
    final older = DateTime(2026, 9, 20, 9);
    expect(relativeTime(older, now: now), JalaliFormatter.formatShortDate(older));
  });

  test('a date from an earlier Jalali year carries the year', () {
    final lastYear = DateTime(2025, 3, 1, 9);
    final label = relativeTime(lastYear, now: now);
    expect(label, startsWith(JalaliFormatter.formatShortDate(lastYear)));
    expect(label, contains('۱۴۰۳'));
  });

  test('a UTC timestamp is compared on the device\'s own calendar', () {
    final utcNow = now.toUtc();
    expect(relativeTime(utcNow.subtract(const Duration(minutes: 5)), now: now), '۵ دقیقه پیش');
  });
}
