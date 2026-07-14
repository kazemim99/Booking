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

  /// e.g. «شنبه ۲۳ تیر»
  static String formatDate(DateTime dateTime) {
    final f = Jalali.fromDateTime(dateTime).formatter;
    return toPersianDigits('${f.wN} ${f.d} ${f.mN}');
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

  /// Weekday name, e.g. «شنبه»
  static String weekday(DateTime dateTime) =>
      Jalali.fromDateTime(dateTime).formatter.wN;
}
