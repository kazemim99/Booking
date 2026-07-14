import 'persian_digits.dart';

/// Canonical phone-number value object for the Provider app.
///
/// SINGLE SOURCE OF TRUTH for parsing, normalization, validation, and
/// formatting of phone numbers. Do NOT duplicate phone logic in screens,
/// blocs, or repositories — depend on this type.
///
/// Confirmed contract (AUTH_SPECIFICATION.md §4.1, D-4):
/// - Canonical format is `09121234567`: exactly 11 digits, starts with `09`.
/// - No spaces, hyphens, or embedded country code.
/// - No automatic `+98`/international conversion inside the app.
/// - The SAME canonical string is sent verbatim to BOTH
///   `send-verification-code` and `provider/complete-authentication`
///   (the backend stores/looks up by an exact normalized `Value`).
class PhoneNumber {
  /// The canonical, validated value, e.g. `09121234567`.
  final String value;

  const PhoneNumber._(this.value);

  /// Iranian mobile in canonical form: `09` + 9 digits (11 total).
  static final RegExp _canonical = RegExp(r'^09\d{9}$');

  /// Normalizes raw user input to the canonical digit string WITHOUT
  /// validating. Converts Persian/Arabic digits, strips spaces/dashes, and
  /// reduces the common `+98…` / `0098…` / `98…` variants to the `09…` form.
  ///
  /// Note: this is defensive normalization for paste/autofill. The UI's
  /// [DigitsOnlyInputFormatter] already keeps typed input clean; the app never
  /// *transmits* a `+98` form — it always transmits the `09…` canonical value.
  static String normalize(String raw) {
    var s = PersianDigits.toWestern(raw).replaceAll(RegExp(r'[\s\-()]'), '');
    if (s.startsWith('+98')) {
      s = '0${s.substring(3)}';
    } else if (s.startsWith('0098')) {
      s = '0${s.substring(4)}';
    } else if (s.startsWith('98') && s.length == 12) {
      s = '0${s.substring(2)}';
    } else if (s.startsWith('9') && s.length == 10) {
      s = '0$s';
    }
    return s;
  }

  /// Returns true if [raw] normalizes to a valid Iranian mobile number.
  static bool isValid(String raw) => _canonical.hasMatch(normalize(raw));

  /// Parses [raw]; returns null when invalid.
  static PhoneNumber? tryParse(String raw) {
    final normalized = normalize(raw);
    if (!_canonical.hasMatch(normalized)) return null;
    return PhoneNumber._(normalized);
  }

  /// Parses [raw]; throws [FormatException] when invalid.
  static PhoneNumber parse(String raw) {
    final result = tryParse(raw);
    if (result == null) {
      throw FormatException('Invalid Iranian mobile number', raw);
    }
    return result;
  }

  /// Masked form for display, e.g. `0912***4567`.
  String get masked =>
      '${value.substring(0, 4)}***${value.substring(value.length - 4)}';

  /// Persian-glyph display form.
  String get displayFa => PersianDigits.toPersian(value);

  @override
  String toString() => value;

  @override
  bool operator ==(Object other) =>
      other is PhoneNumber && other.value == value;

  @override
  int get hashCode => value.hashCode;
}
