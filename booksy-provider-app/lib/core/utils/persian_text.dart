/// Persian text normalization for search/matching.
///
/// Persian city data (and mobile keyboards) mix Arabic and Persian forms of the
/// same letters — most notably kaf (`ك` U+0643 vs `ک` U+06A9) and yeh (`ي`
/// U+064A / `ى` U+0649 vs `ی` U+06CC). A user typing `کاشان` on a phone would
/// never match a record stored as `كاشان`. Normalizing both the query and the
/// candidate to a single canonical form makes search reliable.
class PersianText {
  PersianText._();

  /// Canonicalizes [input] for comparison: unifies kaf/yeh variants, drops
  /// zero-width non-joiners and diacritics, collapses whitespace, and lower-
  /// cases any Latin characters. Not for display — matching only.
  static String normalize(String input) {
    var s = input;
    // Kaf variants → Persian kaf.
    s = s.replaceAll('ك', 'ک');
    // Yeh variants (Arabic yeh, alef maksura) → Persian yeh.
    s = s.replaceAll('ي', 'ی').replaceAll('ى', 'ی');
    // Arabic heh/teh-marbuta occasionally appears for final heh.
    s = s.replaceAll('ة', 'ه');
    // Strip zero-width non-joiner and tatweel (kashida).
    s = s.replaceAll('‌', '').replaceAll('ـ', '');
    // Strip Arabic diacritics (harakat).
    s = s.replaceAll(RegExp('[ً-ْ]'), '');
    // Remove ALL whitespace, not just collapse it: Persian compound place names
    // are written inconsistently (`علی آباد`, `علی‌آباد`, `علیآباد`) and should
    // all match. Lower-case any Latin characters too.
    s = s.replaceAll(RegExp(r'\s+'), '').toLowerCase();
    return s;
  }

  /// True when [candidate] contains [query] after normalizing both.
  static bool contains(String candidate, String query) {
    return normalize(candidate).contains(normalize(query));
  }
}
