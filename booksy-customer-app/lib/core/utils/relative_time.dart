import 'package:shamsi_date/shamsi_date.dart' show Jalali;

import '../constants/app_strings.dart';
import 'jalali_formatter.dart';

/// How long ago [time] was, as a customer reads it in a list: «همین حالا», «۵ دقیقه پیش», «۲ ساعت پیش»,
/// «دیروز», else a short Jalali date (with the year once it is from an earlier Jalali year).
///
/// [now] is always passed in — the caller owns the clock, so this stays pure. Both instants are compared on
/// the device's own calendar, so a UTC timestamp from the server lands on the right day. A time slightly
/// ahead of [now] (a server clock running fast) reads as «همین حالا» rather than as a negative age.
String relativeTime(DateTime time, {required DateTime now}) {
  final at = time.toLocal();
  final current = now.toLocal();
  final age = current.difference(at);

  if (age.inMinutes < 1) return AppStrings.relativeTimeNow;
  if (age.inMinutes < 60) {
    return AppStrings.relativeMinutesAgo(
        JalaliFormatter.toPersianDigits('${age.inMinutes}'));
  }

  final today = DateTime(current.year, current.month, current.day);
  final day = DateTime(at.year, at.month, at.day);
  if (day == today) {
    return AppStrings.relativeHoursAgo(
        JalaliFormatter.toPersianDigits('${age.inHours}'));
  }
  if (day == DateTime(today.year, today.month, today.day - 1)) {
    return AppStrings.relativeYesterday;
  }

  final short = JalaliFormatter.formatShortDate(at);
  final year = Jalali.fromDateTime(at).year;
  if (year != Jalali.fromDateTime(current).year) {
    return '$short ${JalaliFormatter.toPersianDigits('$year')}';
  }
  return short;
}
