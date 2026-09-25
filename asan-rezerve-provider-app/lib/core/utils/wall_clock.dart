/// Reads a booking time the server sent as the salon's wall clock: the DIGITS are the time, whatever zone suffix
/// came with them (FOLLOW-UPS #63). The API wrote "…T10:30:00Z" for a 10:30 booking until 2026-09-23 and "+03:30"
/// before that; parsing either as an instant and converting it to the device's zone showed 14:00 in Tehran — the
/// calendar read "11:00 تا 14:00" for a 10:30–11:00 booking (QA 2026-09-23). The result is a local clock value with
/// exactly the server's digits.
DateTime? tryParseWallClock(String? raw) {
  if (raw == null) return null;
  final digits = raw.trim().replaceFirst(RegExp(r'(Z|[+-]\d{2}:?\d{2})$', caseSensitive: false), '');
  if (digits.isEmpty) return null;
  final parsed = DateTime.tryParse(digits);
  if (parsed == null) return null;
  return DateTime(parsed.year, parsed.month, parsed.day, parsed.hour, parsed.minute, parsed.second,
      parsed.millisecond, parsed.microsecond);
}
