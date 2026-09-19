import 'package:flutter/services.dart';

/// Persian/Arabic ↔ Western digit conversion utilities.
///
/// Digit-glyph normalization ONLY. All phone validation/formatting lives in
/// [PhoneNumber] (single source of truth) — do not duplicate it here.
class PersianDigits {
  PersianDigits._();

  static const _persian = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'];
  static const _arabic = ['٠', '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩'];

  /// Converts any Persian/Arabic digit glyphs in [input] to Western `0-9`.
  static String toWestern(String input) {
    var result = input;
    for (var i = 0; i < 10; i++) {
      result = result
          .replaceAll(_persian[i], '$i')
          .replaceAll(_arabic[i], '$i');
    }
    return result;
  }

  /// Converts Western digits in [input] to Persian glyphs (display only).
  static String toPersian(String input) {
    var result = input;
    for (var i = 0; i < 10; i++) {
      result = result.replaceAll('$i', _persian[i]);
    }
    return result;
  }
}

/// TextInputFormatter that normalizes Persian/Arabic digits to Western and
/// strips any non-digit character as the user types (used by phone & OTP
/// fields so the raw value is always canonical Western digits).
class DigitsOnlyInputFormatter extends TextInputFormatter {
  const DigitsOnlyInputFormatter();

  @override
  TextEditingValue formatEditUpdate(
    TextEditingValue oldValue,
    TextEditingValue newValue,
  ) {
    final normalized = PersianDigits.toWestern(
      newValue.text,
    ).replaceAll(RegExp(r'[^0-9]'), '');
    return TextEditingValue(
      text: normalized,
      selection: TextSelection.collapsed(offset: normalized.length),
    );
  }
}

/// Reading and writing prices, which in this market run to seven digits
/// (۸,۰۰۰,۰۰۰): grouped in threes so they can be read back at a glance and a
/// missing zero is obvious.
class PriceText {
  PriceText._();

  /// Groups the digits of [value] in threes: 250000 -> "250,000".
  static String format(num value) => _group(value.toStringAsFixed(0));

  /// The number in [text], ignoring separators and accepting Persian digits.
  /// Null when there is no digit at all, so an empty field stays empty rather
  /// than silently becoming zero.
  static double? parse(String text) {
    final digits = PersianDigits.toWestern(
      text,
    ).replaceAll(RegExp(r'[^0-9]'), '');
    return digits.isEmpty ? null : double.parse(digits);
  }

  static String _group(String digits) {
    final buffer = StringBuffer();
    for (var i = 0; i < digits.length; i++) {
      if (i > 0 && (digits.length - i) % 3 == 0) buffer.write(',');
      buffer.write(digits[i]);
    }
    return buffer.toString();
  }
}

/// TextInputFormatter that keeps a price field grouped in threes while typing,
/// normalizing Persian digits and dropping everything that is not a digit.
class ThousandsSeparatorInputFormatter extends TextInputFormatter {
  const ThousandsSeparatorInputFormatter();

  @override
  TextEditingValue formatEditUpdate(
    TextEditingValue oldValue,
    TextEditingValue newValue,
  ) {
    final digits = PersianDigits.toWestern(
      newValue.text,
    ).replaceAll(RegExp(r'[^0-9]'), '');
    if (digits.isEmpty) return const TextEditingValue();
    final grouped = PriceText._group(digits);
    return TextEditingValue(
      text: grouped,
      selection: TextSelection.collapsed(offset: grouped.length),
    );
  }
}
