/// A person created by OTP with no name is stored as «مشتری 9384444636» — the word for their side of the
/// marketplace, then their phone digits (the API's `PersonProvisioningService`). The QA walkthrough of
/// 2026-09-22 found a customer showing as «ارائه‌دهنده 9384444636», because that number had first been used
/// on the provider app and signing in as a customer never renames the person.
///
/// Mirrors `PersonName.RealOrNull` on the server: treat those as NO name, and ask for a real one.
const _placeholderFirstNames = {'مشتری', 'ارائه‌دهنده', 'ارائه دهنده'};

bool isPlaceholderName(String? firstName, String? lastName) => realNameOrNull(firstName, lastName) == null;

/// The person's real name, or null when there is none to show.
String? realNameOrNull(String? firstName, String? lastName) {
  var first = firstName?.trim() ?? '';
  var last = lastName?.trim() ?? '';

  if (_placeholderFirstNames.contains(first)) first = '';
  // The placeholder's surname is the phone's national number.
  if (last.isNotEmpty && RegExp(r'^\d+$').hasMatch(last)) last = '';

  final full = '$first $last'.trim();
  return full.isEmpty ? null : full;
}
