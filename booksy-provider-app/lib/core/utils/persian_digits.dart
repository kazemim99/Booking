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
      result = result.replaceAll(_persian[i], '$i').replaceAll(_arabic[i], '$i');
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
    final normalized =
        PersianDigits.toWestern(newValue.text).replaceAll(RegExp(r'[^0-9]'), '');
    return TextEditingValue(
      text: normalized,
      selection: TextSelection.collapsed(offset: normalized.length),
    );
  }
}
