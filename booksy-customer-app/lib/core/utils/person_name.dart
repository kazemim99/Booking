/// A person created by OTP with no name is stored as «مشتری 9384444636» — the word for their side of the
/// marketplace, then their phone digits (the API's `PersonProvisioningService`). The QA walkthrough of
/// 2026-09-22 found a customer showing as «ارائه‌دهنده 9384444636», because that number had first been used
/// on the provider app and signing in as a customer never renames the person; production QA 2026-09-23 found
/// the booking confirm step naming a salon's owner «ارائه‌دهنده 9123135143». "The number must never be written
/// anywhere."
///
/// Mirrors `PersonName` on the server: treat those as NO name, and ask for a real one.
const _placeholderFirstNames = {'مشتری', 'ارائه‌دهنده', 'ارائه دهنده'};

/// A run of digits this long is a phone number, however it is spaced; shorter ones can be part of a name.
const _phoneDigits = 7;

final _whitespace = RegExp(r'\s+');
final _digit = RegExp(r'[0-9۰-۹٠-٩]');
final _digitGroup = RegExp(r'^[0-9۰-۹٠-٩+\-().\/]+$');

bool _isDigitGroup(String token) => _digitGroup.hasMatch(token) && _digit.hasMatch(token);

int _digitCount(String token) => _digit.allMatches(token).length;

bool isPlaceholderName(String? firstName, String? lastName) => realNameOrNull(firstName, lastName) == null;

/// Both a first and a last name, neither of them the OTP placeholder. Booking requires this: the user (QA 2026-09-23)
/// wants «نام و نام خانوادگی» before a salon receives a request, while the sign-up page still accepts a first name
/// alone and may be skipped.
bool hasFullName(String? firstName, String? lastName) {
  final first = firstName?.trim() ?? '';
  final last = lastName?.trim() ?? '';
  if (first.isEmpty || last.isEmpty) return false;
  if (_placeholderFirstNames.contains(first)) return false;
  if (RegExp(r'^\d+$').hasMatch(last)) return false;
  return true;
}

/// The person's real name, or null when there is none to show.
String? realNameOrNull(String? firstName, String? lastName) {
  final parts = realNameParts(firstName, lastName);
  final full = '${parts.first} ${parts.last}'.trim();
  return full.isEmpty ? null : full;
}

/// Each part with the placeholder and any phone number taken out — blank when nothing real is left. What an edit
/// form starts from, so it never offers the number back as a surname.
({String first, String last}) realNameParts(String? firstName, String? lastName) {
  final first = personNameOrNull(firstName) ?? '';
  // The placeholder's surname is the phone's national number: a surname made of digits is not one.
  final rawLast = lastName?.trim() ?? '';
  final last = _isDigitGroup(rawLast) ? '' : personNameOrNull(rawLast) ?? '';
  return (first: first, last: last);
}

/// A name held as one string (a staff member's name, a slot's `availableStaffName`) with any phone number taken
/// out; null when what is left is nothing or the bare placeholder word.
String? personNameOrNull(String? name) {
  if (name == null || name.trim().isEmpty) return null;

  // Split on real whitespace only: the ZWNJ inside «ارائه‌دهنده» is part of the word.
  final kept = <String>[];
  final run = <String>[];

  void endRun() {
    // Consecutive digit groups ("0912 313 5143") are one number; drop it when it is a phone.
    if (run.fold<int>(0, (sum, t) => sum + _digitCount(t)) < _phoneDigits) kept.addAll(run);
    run.clear();
  }

  for (final token in name.trim().split(_whitespace)) {
    if (_isDigitGroup(token)) {
      run.add(token);
      continue;
    }
    endRun();
    kept.add(token);
  }
  endRun();

  final result = kept.join(' ');
  return result.isEmpty || _placeholderFirstNames.contains(result) ? null : result;
}

/// A name sent whole — the booking APIs' `staffName`, «مریم احمدی» — or null when it is only the OTP placeholder
/// («ارائه‌دهنده 9123135143», QA recording 2026-09-23 #8) or a phone number standing where the name should be.
String? realFullNameOrNull(String? fullName) => personNameOrNull(fullName);
