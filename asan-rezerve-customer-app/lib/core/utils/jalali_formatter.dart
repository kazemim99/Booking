import 'package:shamsi_date/shamsi_date.dart';

/// Jalali (Shamsi) date formatting with Persian digits for display.
class JalaliFormatter {
  JalaliFormatter._();

  static const _persianDigits = '۰۱۲۳۴۵۶۷۸۹';

  static String toPersianDigits(String input) {
    final buffer = StringBuffer();
    for (final rune in input.runes) {
      final char = String.fromCharCode(rune);
      final digit = int.tryParse(char);
      buffer.write(digit != null ? _persianDigits[digit] : char);
    }
    return buffer.toString();
  }

  /// Weekday names, Saturday (Jalali weekDay 1) to Friday (7), in the single-word spelling the rest
  /// of the app uses (working hours, business hours). shamsi_date's own `wN` spells some as two
  /// words («یک شنبه», «چهار شنبه»), which read differently from the salon's hours next to them.
  /// «سه‌شنبه» carries a zero-width non-joiner (U+200C).
  static const List<String> _weekdayNames = [
    'شنبه',
    'یکشنبه',
    'دوشنبه',
    'سه‌شنبه',
    'چهارشنبه',
    'پنجشنبه',
    'جمعه',
  ];

  /// e.g. «شنبه ۲۳ تیر»
  static String formatDate(DateTime dateTime) {
    final j = Jalali.fromDateTime(dateTime);
    final f = j.formatter;
    return toPersianDigits('${_weekdayNames[j.weekDay - 1]} ${f.d} ${f.mN}');
  }

  /// e.g. «۱۴:۳۰»
  static String formatTime(DateTime dateTime) {
    final local = dateTime.toLocal();
    final h = local.hour.toString().padLeft(2, '0');
    final m = local.minute.toString().padLeft(2, '0');
    return toPersianDigits('$h:$m');
  }

  /// e.g. «شنبه ۲۳ تیر، ۱۴:۳۰»
  static String formatDateTime(DateTime dateTime) =>
      '${formatDate(dateTime)}، ${formatTime(dateTime)}';

  /// Short day label for pickers, e.g. «۲۳ تیر»
  static String formatShortDate(DateTime dateTime) {
    final f = Jalali.fromDateTime(dateTime).formatter;
    return toPersianDigits('${f.d} ${f.mN}');
  }

  /// Weekday name, e.g. «شنبه», «یکشنبه», «سه‌شنبه» — always a single word.
  static String weekday(DateTime dateTime) =>
      _weekdayNames[Jalali.fromDateTime(dateTime).weekDay - 1];
}
