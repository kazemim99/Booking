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
