import 'package:flutter/services.dart';

/// Normalization for Persian (۰-۹) and Arabic-Indic (٠-٩) numerals.
class PersianDigits {
  PersianDigits._();

  static const _persian = '۰۱۲۳۴۵۶۷۸۹';
  static const _arabic = '٠١٢٣٤٥٦٧٨٩';

  /// Converts any Persian/Arabic numerals in [input] to Western digits.
  static String normalize(String input) {
    final buffer = StringBuffer();
    for (final rune in input.runes) {
      final char = String.fromCharCode(rune);
      final p = _persian.indexOf(char);
      final a = _arabic.indexOf(char);
      if (p >= 0) {
        buffer.write(p);
      } else if (a >= 0) {
        buffer.write(a);
      } else {
        buffer.write(char);
      }
    }
    return buffer.toString();
  }

  /// Normalized digits-only form of [input].
  static String digitsOnly(String input) =>
      normalize(input).replaceAll(RegExp(r'[^\d]'), '');

  /// Valid Iranian mobile number (09xxxxxxxxx, with or without leading 0).
  static bool isValidIranianMobile(String input) {
    final digits = digitsOnly(input);
    return RegExp(r'^(0?9)\d{9}$').hasMatch(digits);
  }

  /// Canonical 11-digit form (adds the leading 0 if missing).
  static String canonicalMobile(String input) {
    final digits = digitsOnly(input);
    return digits.startsWith('9') ? '0$digits' : digits;
  }
}

/// Input formatter that live-normalizes Persian/Arabic numerals as typed.
class PersianDigitsInputFormatter extends TextInputFormatter {
  @override
  TextEditingValue formatEditUpdate(
    TextEditingValue oldValue,
    TextEditingValue newValue,
  ) {
    final normalized = PersianDigits.normalize(newValue.text);
    if (normalized == newValue.text) return newValue;
    return newValue.copyWith(text: normalized);
  }
}
