import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/features/booking/data/repositories/booking_repository_impl.dart';

/// The staff list drives the "choose a team member" step: the bloc shows that step
/// only when more than one active member comes back, and each entry's name is what
/// the customer picks from.
void main() {
  group('parseStaff', () {
    test('prefers fullName so an unclaimed member is not a blank row', () {
      // A member invited by phone who has not signed up yet: the salon typed their
      // name, so it lives only in fullName. Building the label from first+last
      // produced '' and the picker rendered an anonymous row.
      final staff = BookingRepositoryImpl.parseStaff([
        {
          'id': '3772fceb-7f66-4523-9fc8-df06f19816c9',
          'firstName': '',
          'lastName': '',
          'fullName': 'رضا قاسمی',
          'role': 'ServiceProvider',
          'isActive': true,
        },
      ]);

      expect(staff.single.name, 'رضا قاسمی');
      expect(staff.single.name, isNotEmpty,
          reason: 'a nameless entry is unpickable in the staff step');
    });

    test('falls back to first + last when fullName is absent or blank', () {
      final staff = BookingRepositoryImpl.parseStaff([
        {'id': 'a', 'firstName': 'مریم', 'lastName': 'احمدی', 'isActive': true},
        {
          'id': 'b',
          'firstName': 'سارا',
          'lastName': 'رضایی',
          'fullName': '   ',
          'isActive': true,
        },
      ]);

      expect(staff.map((s) => s.name), ['مریم احمدی', 'سارا رضایی']);
    });

    test('keeps id, role and active flag', () {
      final staff = BookingRepositoryImpl.parseStaff([
        {
          'id': 'st-1',
          'fullName': 'نگار',
          'role': 'Owner',
          'isActive': false,
        },
      ]);

      expect(staff.single.id, 'st-1');
      expect(staff.single.role, 'Owner');
      expect(staff.single.isActive, isFalse);
    });

    test('defaults isActive to true when the server omits it', () {
      final staff = BookingRepositoryImpl.parseStaff([
        {'id': 'st-1', 'fullName': 'نگار'},
      ]);

      // activeStaff filters on this, so defaulting to false would silently hide
      // every member and skip the selection step.
      expect(staff.single.isActive, isTrue);
    });

    test('a missing or malformed staff list is empty, not a crash', () {
      expect(BookingRepositoryImpl.parseStaff(null), isEmpty);
      expect(BookingRepositoryImpl.parseStaff('nonsense'), isEmpty);
      expect(BookingRepositoryImpl.parseStaff([]), isEmpty);
    });
  });
}
