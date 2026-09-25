/// Booking times are the SALON'S wall-clock, with no zone: the server lists slots as "2026-09-23T14:00:00" and
/// stores and conflict-checks exactly those digits (FOLLOW-UPS #63).
///
/// So a time goes back to the server as the same digits it came with. Never `toUtc()`: on a device in Tehran
/// that turns a "14:00" slot into 10:30Z, which the server books — and checks for conflicts — at 10:30, leaving
/// the real 14:00 free for someone else (QA walkthrough 2026-09-22; the provider app had the same defect).
String wallClockIso(DateTime t) {
  String two(int v) => v.toString().padLeft(2, '0');
  return '${t.year.toString().padLeft(4, '0')}-${two(t.month)}-${two(t.day)}'
      'T${two(t.hour)}:${two(t.minute)}:${two(t.second)}';
}

/// Reads a booking time the server sent as the salon's wall clock: the DIGITS are the time, whatever zone suffix
/// came with them. The API wrote "…T10:30:00Z" for a 10:30 booking until 2026-09-23 (and "+03:30" before that);
/// parsing that as an instant and calling `toLocal()` showed 14:00 in Tehran (QA 2026-09-23). The result is a local
/// clock value with exactly the server's digits, so it displays as is and goes back through [wallClockIso] unchanged.
DateTime parseWallClock(String raw) {
  final value = tryParseWallClock(raw);
  if (value == null) throw FormatException('Not a date-time', raw);
  return value;
}

/// [parseWallClock], or null when [raw] is absent or not a date-time.
DateTime? tryParseWallClock(String? raw) {
  if (raw == null) return null;
  final digits = raw.trim().replaceFirst(RegExp(r'(Z|[+-]\d{2}:?\d{2})$', caseSensitive: false), '');
  if (digits.isEmpty) return null;
  final parsed = DateTime.tryParse(digits);
  if (parsed == null) return null;
  return DateTime(parsed.year, parsed.month, parsed.day, parsed.hour, parsed.minute, parsed.second,
      parsed.millisecond, parsed.microsecond);
}
