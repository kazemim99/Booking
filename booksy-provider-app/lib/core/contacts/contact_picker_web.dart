import 'dart:js_interop';
import 'dart:js_interop_unsafe';

import '../../features/home/domain/entities/saved_customer.dart';
import 'contact_picker.dart';

/// The browser's Contact Picker API (`navigator.contacts.select`).
ContactPicker createContactPicker() => _BrowserContactPicker();

@JS('navigator')
external JSObject get _navigator;

class _BrowserContactPicker implements ContactPicker {
  JSObject? get _contacts {
    if (!_navigator.has('contacts')) return null;
    final contacts = _navigator.getProperty<JSAny?>('contacts'.toJS);
    if (contacts == null || !(contacts as JSObject).has('select')) return null;
    return contacts;
  }

  @override
  bool get isSupported => _contacts != null;

  @override
  Future<List<CustomerDraft>> pick({bool multiple = true}) async {
    final contacts = _contacts;
    if (contacts == null) return const [];

    final options = JSObject()..setProperty('multiple'.toJS, multiple.toJS);
    try {
      final picked = await contacts
          .callMethod<JSPromise<JSArray<JSObject>>>(
            'select'.toJS,
            ['name'.toJS, 'tel'.toJS].toJS,
            options,
          )
          .toDart;
      return picked.toDart
          .map((c) => ContactPicker.toDraft(_strings(c, 'name'), _strings(c, 'tel')))
          .whereType<CustomerDraft>()
          .toList();
    } catch (_) {
      // Dismissed, or the browser refused (no user gesture): nothing picked.
      return const [];
    }
  }

  static List<String> _strings(JSObject contact, String key) {
    final value = contact.getProperty<JSAny?>(key.toJS);
    if (value == null) return const [];
    return (value as JSArray<JSString>).toDart.map((s) => s.toDart).toList();
  }
}
