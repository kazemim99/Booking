import '../../features/home/domain/entities/saved_customer.dart';
import '../utils/phone_number.dart';
import 'contact_picker_stub.dart'
    if (dart.library.js_interop) 'contact_picker_web.dart' as platform;

/// The phone's own contact picker (spec: provider-customer-book).
///
/// Privacy decision (2026-09-19): only the contacts the provider ticks in the
/// system picker ever reach the app — it never reads the address book. In the
/// browser this is the Contact Picker API, which exists only in Chrome on
/// Android today; everywhere else [isSupported] is false and callers hide the
/// button (typing a customer in always works).
abstract class ContactPicker {
  bool get isSupported;

  /// Opens the system picker; the ticked contacts that carry a usable mobile
  /// number, as drafts. Empty when the provider cancels.
  Future<List<CustomerDraft>> pick({bool multiple = true});

  factory ContactPicker.platform() => platform.createContactPicker();

  /// One picked contact to a draft: its first Iranian mobile number, its name
  /// split into first and last on the first space. Null when it has no mobile.
  static CustomerDraft? toDraft(List<String> names, List<String> phones) {
    final phone = phones
        .map(PhoneNumber.tryParse)
        .firstWhere((p) => p != null, orElse: () => null);
    if (phone == null) return null;

    final name = names
        .map((n) => n.trim().replaceAll(RegExp(r'\s+'), ' '))
        .firstWhere((n) => n.isNotEmpty, orElse: () => '');
    if (name.isEmpty) return CustomerDraft(firstName: phone.value, phone: phone.value);
    final space = name.indexOf(' ');
    return space < 0
        ? CustomerDraft(firstName: name, phone: phone.value)
        : CustomerDraft(
            firstName: name.substring(0, space),
            lastName: name.substring(space + 1),
            phone: phone.value,
          );
  }
}
