import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/features/bookings/domain/entities/booking_summary.dart';
import 'package:booksy_customer_app/features/bookings/domain/repositories/bookings_repository.dart';
import 'package:booksy_customer_app/features/bookings/presentation/bloc/appointments_bloc.dart';

BookingSummary _booking(String id, {String status = 'Confirmed'}) =>
    BookingSummary(
      id: id,
      providerId: 'p1',
      providerName: 'سالن نمونه',
      serviceId: 's1',
      serviceName: 'کوتاهی مو',
      startTime: DateTime.now().add(const Duration(days: 2)),
      durationMinutes: 45,
      price: 250000,
      currency: 'تومان',
      status: status,
      canCancel: true,
      canReschedule: true,
      canReview: false,
    );

class FakeBookingsRepository implements BookingsRepository {
  Either<Failure, List<BookingSummary>>? upcomingResult;
  Either<Failure, List<BookingSummary>>? pastResult;
  Either<Failure, Unit>? cancelResult;
  Either<Failure, Unit>? rescheduleResult;
  int cancelCalls = 0;

  @override
  Future<Either<Failure, List<BookingSummary>>> getMyBookings({
    required bool upcoming,
    int pageSize = 50,
  }) async =>
      upcoming ? upcomingResult! : pastResult!;

  @override
  Future<Either<Failure, Unit>> cancelBooking({
    required String bookingId,
    required String reason,
  }) async {
    cancelCalls++;
    return cancelResult!;
  }

  @override
  Future<Either<Failure, Unit>> rescheduleBooking({
    required String bookingId,
    required DateTime newStartTime,
    String? newStaffId,
  }) async =>
      rescheduleResult!;
}

Future<void> _pump() => Future<void>.delayed(const Duration(milliseconds: 20));

void main() {
  group('AppointmentsBloc', () {
    test('loads upcoming and past lists', () async {
      final repo = FakeBookingsRepository()
        ..upcomingResult = Right([_booking('b1')])
        ..pastResult = Right([_booking('b0', status: 'Completed')]);
      final bloc = AppointmentsBloc(repo);

      bloc.add(const AppointmentsRequested());
      await _pump();

      expect(bloc.state.status, AppointmentsStatus.loaded);
      expect(bloc.state.upcoming.single.id, 'b1');
      expect(bloc.state.past.single.id, 'b0');
      await bloc.close();
    });

    test('empty lists yield empty status with explore CTA state', () async {
      final repo = FakeBookingsRepository()
        ..upcomingResult = const Right([])
        ..pastResult = const Right([]);
      final bloc = AppointmentsBloc(repo);

      bloc.add(const AppointmentsRequested());
      await _pump();

      expect(bloc.state.status, AppointmentsStatus.empty);
      await bloc.close();
    });

    test('both requests failing yields error status', () async {
      final repo = FakeBookingsRepository()
        ..upcomingResult = const Left(ServerFailure('down'))
        ..pastResult = const Left(ServerFailure('down'));
      final bloc = AppointmentsBloc(repo);

      bloc.add(const AppointmentsRequested());
      await _pump();

      expect(bloc.state.status, AppointmentsStatus.error);
      expect(bloc.state.errorMessage, 'down');
      await bloc.close();
    });

    test('cancel success updates the card optimistically', () async {
      final repo = FakeBookingsRepository()
        ..upcomingResult = Right([_booking('b1')])
        ..pastResult = const Right([])
        ..cancelResult = const Right(unit);
      final bloc = AppointmentsBloc(repo);

      bloc.add(const AppointmentsRequested());
      await _pump();
      bloc.add(AppointmentCancelled(bloc.state.upcoming.single));
      await _pump();

      final card = bloc.state.upcoming.single;
      expect(card.status, 'Cancelled');
      expect(card.canCancel, isFalse);
      expect(bloc.state.notice, AppointmentsNotice.cancelSuccess);
      expect(repo.cancelCalls, 1);
      await bloc.close();
    });

    test('cancel failure rolls the card back', () async {
      final repo = FakeBookingsRepository()
        ..upcomingResult = Right([_booking('b1')])
        ..pastResult = const Right([])
        ..cancelResult = const Left(ServerFailure('نشد'));
      final bloc = AppointmentsBloc(repo);

      bloc.add(const AppointmentsRequested());
      await _pump();

      final noticed = bloc.stream
          .firstWhere((s) => s.notice == AppointmentsNotice.cancelFailure);
      bloc.add(AppointmentCancelled(bloc.state.upcoming.single));
      final failureState = await noticed;

      expect(failureState.upcoming.single.status, 'Confirmed');
      expect(failureState.upcoming.single.canCancel, isTrue);
      await bloc.close();
    });

    test('reschedule event updates the card start time in place', () async {
      final repo = FakeBookingsRepository()
        ..upcomingResult = Right([_booking('b1')])
        ..pastResult = const Right([]);
      final bloc = AppointmentsBloc(repo);

      bloc.add(const AppointmentsRequested());
      await _pump();

      final newTime = DateTime.now().add(const Duration(days: 5));
      bloc.add(AppointmentRescheduled('b1', newTime));
      await _pump();

      expect(bloc.state.upcoming.single.startTime, newTime);
      await bloc.close();
    });
  });
}
