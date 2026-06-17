import 'package:shamsi_date/shamsi_date.dart';
import 'persian_formatter.dart';

/// Jalali (Persian) date formatting utilities
class DateFormatter {
  DateFormatter._();

  static const Map<int, String> _persianWeekdays = {
    1: 'دوشنبه',
    2: 'سه‌شنبه',
    3: 'چهارشنبه',
    4: 'پنجشنبه',
    5: 'جمعه',
    6: 'شنبه',
    7: 'یکشنبه',
  };

  static const Map<int, String> _persianMonths = {
    1: 'فروردین',
    2: 'اردیبهشت',
    3: 'خرداد',
    4: 'تیر',
    5: 'مرداد',
    6: 'شهریور',
    7: 'مهر',
    8: 'آبان',
    9: 'آذر',
    10: 'دی',
    11: 'بهمن',
    12: 'اسفند',
  };

  /// Convert DateTime to Jalali date
  static Jalali toJalali(DateTime dateTime) {
    return Jalali.fromDateTime(dateTime);
  }

  /// Format date as "چهارشنبه، ۱۲ فروردین ۱۴۰۳"
  static String formatFull(DateTime dateTime) {
    final jalali = toJalali(dateTime);
    final weekday = _persianWeekdays[jalali.weekDay] ?? '';
    final day = PersianFormatter.intToPersian(jalali.day);
    final month = _persianMonths[jalali.month] ?? '';
    final year = PersianFormatter.intToPersian(jalali.year);

    return '$weekday، $day $month $year';
  }

  /// Format date as "۱۲ فروردین"
  static String formatShort(DateTime dateTime) {
    final jalali = toJalali(dateTime);
    final day = PersianFormatter.intToPersian(jalali.day);
    final month = _persianMonths[jalali.month] ?? '';

    return '$day $month';
  }

  /// Format date as "۱۴۰۳/۰۱/۱۲"
  static String formatNumeric(DateTime dateTime) {
    final jalali = toJalali(dateTime);
    final year = jalali.year.toString().padLeft(4, '0');
    final month = jalali.month.toString().padLeft(2, '0');
    final day = jalali.day.toString().padLeft(2, '0');

    return PersianFormatter.toPersianDigits('$year/$month/$day');
  }

  /// Format time as "۱۴:۳۰"
  static String formatTime(DateTime dateTime) {
    final hour = dateTime.hour.toString().padLeft(2, '0');
    final minute = dateTime.minute.toString().padLeft(2, '0');

    return PersianFormatter.toPersianDigits('$hour:$minute');
  }

  /// Format date and time as "چهارشنبه، ۱۲ فروردین - ۱۴:۳۰"
  static String formatDateTime(DateTime dateTime) {
    return '${formatFull(dateTime)} - ${formatTime(dateTime)}';
  }

  /// Get weekday name
  static String getWeekdayName(DateTime dateTime) {
    final jalali = toJalali(dateTime);
    return _persianWeekdays[jalali.weekDay] ?? '';
  }

  /// Get month name
  static String getMonthName(DateTime dateTime) {
    final jalali = toJalali(dateTime);
    return _persianMonths[jalali.month] ?? '';
  }
}

/// Extension methods for DateTime
extension PersianDateTimeExtension on DateTime {
  /// Convert to Jalali
  Jalali toJalali() => DateFormatter.toJalali(this);

  /// Format as full Persian date
  String formatPersianFull() => DateFormatter.formatFull(this);

  /// Format as short Persian date
  String formatPersianShort() => DateFormatter.formatShort(this);

  /// Format as numeric Persian date
  String formatPersianNumeric() => DateFormatter.formatNumeric(this);

  /// Format time in Persian
  String formatPersianTime() => DateFormatter.formatTime(this);

  /// Format date and time in Persian
  String formatPersianDateTime() => DateFormatter.formatDateTime(this);

  /// Get Persian weekday name
  String getPersianWeekday() => DateFormatter.getWeekdayName(this);

  /// Get Persian month name
  String getPersianMonth() => DateFormatter.getMonthName(this);
}
