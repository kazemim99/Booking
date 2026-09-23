import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/features/booking/data/repositories/booking_repository_impl.dart';
import 'package:booksy_customer_app/features/booking/domain/entities/booking_entities.dart';

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

    // Production QA 2026-09-23: the confirm step named the salon's owner «ارائه‌دهنده 9123135143». A single-member
    // salon skips the staff step and uses this entry's name, so it must never be a placeholder or a phone.
    test('a placeholder or a phone is never a member name: the salon name stands in', () {
      final staff = BookingRepositoryImpl.parseStaff([
        {'id': 'a', 'firstName': 'ارائه‌دهنده', 'lastName': '9123135143', 'fullName': ''},
        {'id': 'b', 'fullName': 'ارائه‌دهنده 9123135143'},
        {'id': 'c', 'fullName': '09123135143'},
        {'id': 'd', 'firstName': 'مشتری', 'lastName': '9384444636'},
      ], fallbackName: 'سالن نهال');

      expect(staff.map((s) => s.name), everyElement('سالن نهال'));
      expect(staff.map((s) => s.name).join(), isNot(contains('9123135143')));
    });

    test('a real name next to a phone keeps the name only', () {
      final staff = BookingRepositoryImpl.parseStaff([
        {'id': 'a', 'firstName': 'سارا', 'lastName': '9123135143'},
      ], fallbackName: 'سالن نهال');

      expect(staff.single.name, 'سارا');
    });

    test("a slot's staff name that is a placeholder or a phone is no name", () {
      TimeSlot slotNamed(String? name) => BookingRepositoryImpl.parseSlot({
            'startTime': '2026-09-24T10:00:00Z',
            'endTime': '2026-09-24T10:45:00Z',
            'durationMinutes': 45,
            'isAvailable': true,
            'availableStaffId': 'st1',
            'availableStaffName': name,
          });

      expect(slotNamed('ارائه‌دهنده 9123135143').staffName, isNull);
      expect(slotNamed('09123135143').staffName, isNull);
      expect(slotNamed('مریم احمدی').staffName, 'مریم احمدی');
      expect(slotNamed(null).staffName, isNull);
    });

    test('a missing or malformed staff list is empty, not a crash', () {
      expect(BookingRepositoryImpl.parseStaff(null), isEmpty);
      expect(BookingRepositoryImpl.parseStaff('nonsense'), isEmpty);
      expect(BookingRepositoryImpl.parseStaff([]), isEmpty);
    });
  });
}