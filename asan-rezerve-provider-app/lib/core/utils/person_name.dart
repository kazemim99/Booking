/// Splits and joins a person's name for the single "full name" field.
///
/// The UI asks for one full name, but the backend stores — and requires — first and last
/// name separately (SaveStep3LocationCommandHandler rejects either being blank). So the
/// full name is split before it is sent.
///
/// **Rule: split on the LAST run of whitespace.** Everything before it is the first name,
/// the final token is the last name. Chosen for Persian:
/// - compound first names are commonly written with a real space — «محمد علی رضایی» →
///   first «محمد علی», last «رضایی» (splitting on the FIRST space would get this wrong);
/// - compound surnames are conventionally joined with a ZERO WIDTH NON-JOINER (نیم‌فاصله,
///   U+200C), which is not whitespace, so «علی حسینی‌نژاد» keeps «حسینی‌نژاد» whole.
///
/// The app always displays `first + " " + last`, so even a surname written with a real
/// space (a mis-split) still reconstructs exactly what the user typed.
class PersonName {
  PersonName._();

  // Regular whitespace only. U+200C (ZWNJ) is a format character, not whitespace, and must
  // never be treated as a separator.
  static final RegExp _whitespace = RegExp(r'[\s ]+');

  /// Returns `(first, last)`, or `null` when the input has fewer than two words (there is
  /// no last name to send, and the backend would reject the request).
  static ({String first, String last})? split(String fullName) {
    final tokens = fullName
        .trim()
        .split(_whitespace)
        .where((t) => t.isNotEmpty)
        .toList();
    if (tokens.length < 2) return null;
    return (
      first: tokens.sublist(0, tokens.length - 1).join(' '),
      last: tokens.last,
    );
  }

  /// Rebuilds the single field from stored parts (e.g. when a saved draft is restored).
  static String join(String first, String last) =>
      [first.trim(), last.trim()].where((p) => p.isNotEmpty).join(' ');

  // ---- What counts as a real name (mirrors PersonName on the server) ----
  //
  // Phone sign-in names a person «ارائه‌دهنده 9123135143» — the word for their side of the marketplace, then their
  // phone's national number — until they give a name. Production QA 2026-09-23 found the salon app showing that,
  // and the bare phone, as the owner's name: "the number must never be written anywhere".

  static const _placeholderWords = {'مشتری', 'ارائه‌دهنده', 'ارائه دهنده'};

  /// A run of digits this long is a phone number, however it is spaced; shorter ones can be part of a name.
  static const _phoneDigits = 7;

  static final RegExp _digit = RegExp(r'[0-9\u06F0-\u06F9\u0660-\u0669]');
  static final RegExp _digitGroup = RegExp(r'^[0-9\u06F0-\u06F9\u0660-\u0669+\-().\/]+$');

  static bool _isDigitGroup(String token) =>
      _digitGroup.hasMatch(token) && _digit.hasMatch(token);

  /// The person's real name from their two parts, or null when they have none.
  static String? realOrNull(String? firstName, String? lastName) {
    final first = sanitize(firstName) ?? '';
    // The placeholder's surname is the phone's national number: a surname made of digits is not one.
    final rawLast = lastName?.trim() ?? '';
    final last = _isDigitGroup(rawLast) ? '' : sanitize(rawLast) ?? '';
    final full = join(first, last);
    return full.isEmpty ? null : full;
  }

  /// A name held as one string with any phone number taken out; null when what is left is nothing or the bare
  /// placeholder word.
  static String? sanitize(String? name) {
    if (name == null || name.trim().isEmpty) return null;

    final kept = <String>[];
    final run = <String>[];
    void endRun() {
      // Consecutive digit groups ("0912 313 5143") are one number; drop it when it is a phone.
      final digits = run.fold<int>(0, (n, t) => n + _digit.allMatches(t).length);
      if (digits < _phoneDigits) kept.addAll(run);
      run.clear();
    }

    for (final token in name.trim().split(_whitespace)) {
      if (token.isEmpty) continue;
      if (_isDigitGroup(token)) {
        run.add(token);
        continue;
      }
      endRun();
      kept.add(token);
    }
    endRun();

    final result = kept.join(' ');
    return result.isEmpty || _placeholderWords.contains(result) ? null : result;
  }
}
