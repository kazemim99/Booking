import 'package:asan_rezerve_provider_app/core/contacts/contact_picker.dart';
import 'package:asan_rezerve_provider_app/core/utils/phone_number.dart';
import 'package:asan_rezerve_provider_app/features/home/domain/entities/saved_customer.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('ContactPicker.toDraft (one ticked contact to a customer)', () {
    test('splits the name on the first space and keeps the mobile canonical', () {
      expect(
        ContactPicker.toDraft(['مرتضی کاظمی نژاد'], ['+98 912 313 5143']),
        const CustomerDraft(
            firstName: 'مرتضی', lastName: 'کاظمی نژاد', phone: '09123135143'),
      );
    });

    test('takes the first number that is an Iranian mobile', () {
      final draft = ContactPicker.toDraft(['سارا'], ['021 8888 7777', '0935-111-2233']);
      expect(draft!.phone, '09351112233');
      expect(draft.firstName, 'سارا');
      expect(draft.lastName, isEmpty);
    });

    test('a contact with no mobile is left out', () {
      expect(ContactPicker.toDraft(['دفتر'], ['021 8888 7777']), isNull);
      expect(ContactPicker.toDraft(['بی‌شماره'], const []), isNull);
    });

    test('a contact with no name is named by its number', () {
      final draft = ContactPicker.toDraft(const [], ['۰۹۱۲۳۱۳۵۱۴۳']);
      expect(draft!.firstName, '09123135143');
    });
  });

  test('PhoneNumber.display groups a mobile for reading, whatever its spelling', () {
    expect(PhoneNumber.display('+989123135143'), '0912 313 5143');
    expect(PhoneNumber.display('09123135143'), '0912 313 5143');
    expect(PhoneNumber.display('021 8888 7777'), '021 8888 7777');
  });
}
