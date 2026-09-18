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
}
