/// Persian number and text formatting utilities
class PersianFormatter {
  PersianFormatter._();

  static const Map<String, String> _englishToPersianDigits = {
    '0': '۰',
    '1': '۱',
    '2': '۲',
    '3': '۳',
    '4': '۴',
    '5': '۵',
    '6': '۶',
    '7': '۷',
    '8': '۸',
    '9': '۹',
  };

  /// Convert English digits to Persian
  /// Example: "123" → "۱۲۳"
  static String toPersianDigits(String input) {
    String result = input;
    _englishToPersianDigits.forEach((eng, per) {
      result = result.replaceAll(eng, per);
    });
    return result;
  }

  /// Convert integer to Persian string
  /// Example: 123 → "۱۲۳"
  static String intToPersian(int number) {
    return toPersianDigits(number.toString());
  }

  /// Format number with thousands separator
  /// Example: 1234567 → "۱٬۲۳۴٬۵۶۷"
  static String formatNumber(int number) {
    final parts = <String>[];
    String numStr = number.toString();

    // Add thousands separators from right to left
    while (numStr.length > 3) {
      parts.insert(0, numStr.substring(numStr.length - 3));
      numStr = numStr.substring(0, numStr.length - 3);
    }
    if (numStr.isNotEmpty) {
      parts.insert(0, numStr);
    }

    final formatted = parts.join('٬'); // Persian comma
    return toPersianDigits(formatted);
  }
}

/// Extension methods for String
extension PersianStringExtension on String {
  /// Convert English digits in this string to Persian
  String toPersianDigits() => PersianFormatter.toPersianDigits(this);
}

/// Extension methods for int
extension PersianIntExtension on int {
  /// Convert this int to Persian string
  String toPersianString() => PersianFormatter.intToPersian(this);

  /// Format this int with thousands separator
  String formatPersian() => PersianFormatter.formatNumber(this);
}
