import 'package:booksy_provider_app/features/home/domain/entities/home_booking.dart';
import 'package:flutter_test/flutter_test.dart';

/// «تکمیل» and «عدم حضور» are offered only when the server takes them — `Booking.Complete` (a confirmed booking,
/// from 15 minutes before its start) and `Booking.MarkAsNoShow` (a confirmed booking whose time is over). Marking a
/// visit done is what lets its customer review it (openspec/changes/_inline/customer-reviews-and-nahal-seed).
void main() {
  final start = DateTime(2026, 9, 25, 10);
  HomeBooking at(HomeBookingStatus status, {DateTime? end}) => HomeBooking(
        id: 'b1',
        start: start,
        end: end ?? start.add(const Duration(minutes: 45)),
        clientName: 'ناصر عابدی',
        serviceName: 'اصلاح کامل',
        status: status,
      );

  group('تکمیل', () {
    test('opens fifteen minutes before the start', () {
      final b = at(HomeBookingStatus.confirmed);
      expect(b.canCompleteAt(start.subtract(const Duration(minutes: 16))), isFalse);
      expect(b.canCompleteAt(start.subtract(const Duration(minutes: 15))), isTrue);
      expect(b.canCompleteAt(start.add(const Duration(hours: 5))), isTrue, reason: 'no upper limit');
    });

    test('never for a request not yet confirmed, or a visit already settled', () {
      final later = start.add(const Duration(hours: 1));
      for (final status in [
        HomeBookingStatus.pending,
        HomeBookingStatus.completed,
        HomeBookingStatus.noShow,
        HomeBookingStatus.cancelled,
      ]) {
        expect(at(status).canCompleteAt(later), isFalse, reason: '$status');
      }
    });
  });

  group('عدم حضور', () {
    test('once the time is over', () {
      final b = at(HomeBookingStatus.confirmed);
      expect(b.canMarkNoShowAt(start.add(const Duration(minutes: 44))), isFalse);
      expect(b.canMarkNoShowAt(start.add(const Duration(minutes: 45))), isTrue);
    });

    test('a booking without an end is over at its start', () {
      final b = HomeBooking(
          id: 'b1', start: start, clientName: 'x', serviceName: 'y', status: HomeBookingStatus.confirmed);
      expect(b.canMarkNoShowAt(start), isTrue);
    });

    test('never for a pending request', () {
      expect(at(HomeBookingStatus.pending).canMarkNoShowAt(start.add(const Duration(hours: 2))), isFalse);
    });
  });
}
