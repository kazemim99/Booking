import 'dart:async';

import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/features/bookings/domain/entities/booking_summary.dart';
import 'package:booksy_customer_app/features/bookings/domain/repositories/bookings_repository.dart';
import 'package:booksy_customer_app/features/bookings/presentation/bloc/appointments_bloc.dart';
import 'package:booksy_customer_app/features/reviews/domain/entities/review.dart';

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

  /// When set, list reads answer only once it completes, with the lists as
  /// they were when the read was sent (a slow network).
  Completer<void>? listGate;

  /// When set, a cancel is answered only once it completes.
  Completer<void>? cancelGate;

  @override
  Future<Either<Failure, List<BookingSummary>>> getMyBookings({
    required bool upcoming,
    int pageSize = 50,
  }) async {
    final answer = upcoming ? upcomingResult! : pastResult!;
    await listGate?.future;
    return answer;
  }

  @override
  Future<Either<Failure, BookingSummary>> getBookingById(String bookingId) async =>
      const Left(ServerFailure('not used by the list'));

  @override
  Future<Either<Failure, Unit>> cancelBooking({
    required String bookingId,
    required String reason,
  }) async {
    cancelCalls++;
    await cancelGate?.future;
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

/// Lets queued events and answered futures run, without the wall clock.
Future<void> _settle() async {
  for (var i = 0; i < 50; i++) {
    await Future<void>.value();
  }
}

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

    test('a refresh on return from a booking keeps the list on screen and picks up changes', () async {
      final repo = FakeBookingsRepository()
        ..upcomingResult = Right([_booking('b1')])
        ..pastResult = const Right([]);
      final bloc = AppointmentsBloc(repo);

      bloc.add(const AppointmentsRequested());
      await _pump();

      // Cancelled on the detail screen meanwhile.
      repo.upcomingResult = Right([
        _booking('b1').copyWith(status: 'Cancelled', canCancel: false, canReschedule: false),
      ]);
      final statuses = <AppointmentsStatus>[];
      final sub = bloc.stream.listen((s) => statuses.add(s.status));
      bloc.add(const AppointmentsRefreshed());
      await _pump();
      await sub.cancel();

      expect(statuses, isNot(contains(AppointmentsStatus.loading)));
      expect(bloc.state.upcoming.single.status, 'Cancelled');
      await bloc.close();
    });

    group('a refresh that overlaps a change on this screen', () {
      final cancelled = _booking('b1').copyWith(status: 'Cancelled', canCancel: false, canReschedule: false);

      Future<(FakeBookingsRepository, AppointmentsBloc)> loaded() async {
        final repo = FakeBookingsRepository()
          ..upcomingResult = Right([_booking('b1')])
          ..pastResult = const Right([])
          ..cancelResult = const Right(unit);
        final bloc = AppointmentsBloc(repo)..add(const AppointmentsRequested());
        await _settle();
        return (repo, bloc);
      }

      test('a refresh read before a cancel does not offer cancel again when it lands after', () async {
        final (repo, bloc) = await loaded();

        repo.listGate = Completer<void>();
        bloc.add(const AppointmentsRefreshed());
        await _settle();
        // The server now has it cancelled, but the refresh already read it.
        bloc.add(AppointmentCancelled(bloc.state.upcoming.single));
        await _settle();
        repo
          ..upcomingResult = Right([cancelled])
          ..listGate!.complete();
        await _settle();

        expect(bloc.state.upcoming.single.status, 'Cancelled');
        expect(bloc.state.upcoming.single.canCancel, isFalse);
        await bloc.close();
      });

      test('a refresh that starts and lands while a cancel is in flight does not undo it', () async {
        final (repo, bloc) = await loaded();

        repo.cancelGate = Completer<void>();
        bloc.add(AppointmentCancelled(bloc.state.upcoming.single));
        await _settle();
        bloc.add(const AppointmentsRefreshed());
        await _settle();
        repo.cancelGate!.complete();
        await _settle();

        expect(bloc.state.upcoming.single.status, 'Cancelled');
        expect(bloc.state.upcoming.single.canCancel, isFalse);
        await bloc.close();
      });

      test('a refresh read before a reschedule does not bring the old time back', () async {
        final (repo, bloc) = await loaded();
        final newTime = bloc.state.upcoming.single.startTime.add(const Duration(days: 1));

        repo.listGate = Completer<void>();
        bloc.add(const AppointmentsRefreshed());
        await _settle();
        bloc.add(AppointmentRescheduled('b1', newTime));
        await _settle();
        repo.listGate!.complete();
        await _settle();

        expect(bloc.state.upcoming.single.startTime, newTime);
        await bloc.close();
      });

      test('a refresh after the change has settled is applied', () async {
        final (repo, bloc) = await loaded();

        bloc.add(AppointmentCancelled(bloc.state.upcoming.single));
        await _settle();
        // Completed elsewhere meanwhile: the server's copy wins.
        repo.upcomingResult = Right([_booking('b1', status: 'Completed')]);
        bloc.add(const AppointmentsRefreshed());
        await _settle();

        expect(bloc.state.upcoming.single.status, 'Completed');
        await bloc.close();
      });
    });

    test('a review saved from a past card shows it as written, and a refresh read before it does not undo it',
        () async {
      final done = BookingSummary(
        id: 'b0',
        providerId: 'p1',
        providerName: 'سالن نمونه',
        serviceId: 's1',
        serviceName: 'کوتاهی مو',
        startTime: DateTime(2026, 5, 10, 14),
        durationMinutes: 45,
        price: 250000,
        currency: 'تومان',
        status: 'Completed',
        canCancel: false,
        canReschedule: false,
        canReview: true,
      );
      final repo = FakeBookingsRepository()
        ..upcomingResult = const Right([])
        ..pastResult = Right([done]);
      final bloc = AppointmentsBloc(repo);
      bloc.add(const AppointmentsRequested());
      await _settle();

      repo.listGate = Completer<void>();
      bloc.add(const AppointmentsRefreshed());
      await _settle();
      bloc.add(const AppointmentReviewed('b0'));
      await _settle();
      repo.listGate!.complete();
      await _settle();

      final card = bloc.state.past.single;
      expect(card.canReview, isFalse);
      expect(card.reviewStatus, ReviewModerationStatus.pending);
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
