import 'package:flutter_test/flutter_test.dart';

import 'package:asan_rezerve_customer_app/core/errors/failures.dart';
import 'package:asan_rezerve_customer_app/features/bookings/presentation/bloc/appointment_detail_cubit.dart';

import 'bookings_fakes.dart';

/// The appointment detail screen (UX review 2026-09-23, E.1/E.2/E.6): it is opened from the home "next booking"
/// card and from notifications, so it must find any of the customer's bookings and act on it like the list does.
void main() {
  Future<AppointmentDetailState> settled(AppointmentDetailCubit cubit) async {
    // Every fake answers synchronously in a microtask; a few turns let a load/refresh chain finish.
    for (var i = 0; i < 10; i++) {
      await Future<void>.value();
    }
    return cubit.state;
  }

  group('finding the booking', () {
    test('a booking in the lists is shown without asking for it by id', () async {
      final repo = FakeBookings(upcoming: [fakeBooking('b1')]);
      final cubit = AppointmentDetailCubit(repo, 'b1')..load();

      final state = await settled(cubit);

      expect(state.status, AppointmentDetailStatus.loaded);
      expect(state.booking!.id, 'b1');
      expect(repo.byIdCalls, isEmpty);
      await cubit.close();
    });

    test('a booking older than both lists is read by id', () async {
      final repo = FakeBookings(
        past: [fakeBooking('b-new', status: 'Completed')],
        onlyById: {'b-old': fakeBooking('b-old', status: 'Completed')},
      );
      final cubit = AppointmentDetailCubit(repo, 'b-old')..load();

      final state = await settled(cubit);

      expect(state.status, AppointmentDetailStatus.loaded);
      expect(state.booking!.id, 'b-old');
      expect(repo.byIdCalls, ['b-old']);
      await cubit.close();
    });

    test('a booking the server will not give (e.g. 403) is the error state', () async {
      final repo = FakeBookings()..byIdFailure = const UnauthorizedFailure('forbidden');
      final cubit = AppointmentDetailCubit(repo, 'b-x')..load();

      final state = await settled(cubit);

      expect(state.status, AppointmentDetailStatus.error);
      expect(state.errorMessage, isNotEmpty);
      await cubit.close();
    });
  });

  group('cancel', () {
    test('success tells the customer and shows the refreshed booking', () async {
      final repo = FakeBookings(upcoming: [fakeBooking('b1')]);
      final cubit = AppointmentDetailCubit(repo, 'b1')..load();
      await settled(cubit);

      final notices = <AppointmentDetailNotice>[];
      final sub = cubit.stream.listen((s) => notices.add(s.notice));
      await cubit.cancel();
      final state = await settled(cubit);
      await sub.cancel();

      expect(repo.cancelCalls, ['b1']);
      expect(notices, contains(AppointmentDetailNotice.cancelSuccess));
      expect(state.status, AppointmentDetailStatus.loaded);
      expect(state.booking!.status, 'Cancelled');
      expect(state.booking!.canCancel, isFalse);
      expect(state.booking!.canReschedule, isFalse);
      await cubit.close();
    });

    test('failure tells the customer and leaves the booking as it was', () async {
      final repo = FakeBookings(upcoming: [fakeBooking('b1')])..cancelFailure = const ServerFailure('نشد');
      final cubit = AppointmentDetailCubit(repo, 'b1')..load();
      await settled(cubit);

      final notices = <AppointmentDetailNotice>[];
      final sub = cubit.stream.listen((s) => notices.add(s.notice));
      await cubit.cancel();
      final state = await settled(cubit);
      await sub.cancel();

      expect(notices, contains(AppointmentDetailNotice.cancelFailure));
      expect(state.booking!.status, 'Confirmed');
      expect(state.booking!.canCancel, isTrue);
      await cubit.close();
    });

    test('a cancel whose refresh fails still shows the booking as cancelled', () async {
      final repo = FakeBookings(upcoming: [fakeBooking('b1')]);
      final cubit = AppointmentDetailCubit(repo, 'b1')..load();
      await settled(cubit);

      repo
        ..listFailure = const NetworkFailure('offline')
        ..byIdFailure = const NetworkFailure('offline');
      await cubit.cancel();
      final state = await settled(cubit);

      expect(state.status, AppointmentDetailStatus.loaded);
      expect(state.booking!.status, 'Cancelled');
      expect(state.booking!.canCancel, isFalse);
      await cubit.close();
    });
  });

  test('a reschedule shows the new time', () async {
    final repo = FakeBookings(upcoming: [fakeBooking('b1')]);
    final cubit = AppointmentDetailCubit(repo, 'b1')..load();
    await settled(cubit);

    final newStart = DateTime(2026, 9, 27, 11);
    await repo.rescheduleBooking(bookingId: 'b1', newStartTime: newStart);
    await cubit.rescheduled(newStart);
    final state = await settled(cubit);

    expect(state.booking!.startTime, newStart);
    // The new time waits for the salon again (reviews-and-reschedule-round2 item 9).
    expect(state.booking!.status, 'Requested');
    await cubit.close();
  });

  // reviews-and-reschedule-round2 item 9: the server closes the booking and opens a new one; the screen follows it.
  test('after a reschedule the screen follows the new booking, awaiting the salon', () async {
    final repo = FakeBookings(upcoming: [fakeBooking('b1')]);
    final cubit = AppointmentDetailCubit(repo, 'b1')..load();
    await settled(cubit);

    final newStart = DateTime(2026, 9, 27, 11);
    final newId = (await repo.rescheduleBooking(bookingId: 'b1', newStartTime: newStart))
        .getOrElse(() => throw 'x');
    await cubit.rescheduled(newStart, newBookingId: newId);
    final state = await settled(cubit);

    expect(cubit.bookingId, 'b1-moved');
    expect(state.booking!.id, 'b1-moved', reason: 'the refresh read the new booking, not the closed one');
    expect(state.booking!.status, 'Requested');
    expect(state.booking!.startTime, newStart);

    // A reload (pull to refresh, retry) stays on the moved booking too.
    await cubit.load();
    expect((await settled(cubit)).booking!.status, 'Requested');
    await cubit.close();
  });

  test('a reschedule whose answer names no new booking keeps showing the moved time, not the closed booking',
      () async {
    final repo = FakeBookings(upcoming: [fakeBooking('b1')])..namesNewBooking = false;
    final cubit = AppointmentDetailCubit(repo, 'b1')..load();
    await settled(cubit);

    final newStart = DateTime(2026, 9, 27, 11);
    final newId = (await repo.rescheduleBooking(bookingId: 'b1', newStartTime: newStart))
        .getOrElse(() => throw 'x');
    expect(newId, isNull);
    await cubit.rescheduled(newStart, newBookingId: newId);
    final state = await settled(cubit);

    expect(state.booking!.status, 'Requested', reason: 'never the old booking\'s «Rescheduled»');
    expect(state.booking!.startTime, newStart);
    await cubit.close();
  });

  test('after a review in this session the review action is no longer offered', () async {
    final repo = FakeBookings(past: [fakeBooking('b0', status: 'Completed')]);
    final cubit = AppointmentDetailCubit(repo, 'b0')..load();
    await settled(cubit);
    expect(cubit.state.canWriteReview, isTrue);

    cubit.reviewed();

    expect(cubit.state.canWriteReview, isFalse);
    await cubit.close();
  });

  test('a review saved in this session stays saved when the booking is read again', () async {
    // The server's copy can still say canReview for a moment after the review is saved.
    final repo = FakeBookings(past: [fakeBooking('b0', status: 'Completed')]);
    final cubit = AppointmentDetailCubit(repo, 'b0')..load();
    await settled(cubit);
    cubit.reviewed();

    await cubit.load();

    expect(cubit.state.status, AppointmentDetailStatus.loaded);
    expect(cubit.state.reviewed, isTrue);
    expect(cubit.state.canWriteReview, isFalse);
    await cubit.close();
  });

  test('a review saved in this session survives a failed read and its retry', () async {
    final repo = FakeBookings(past: [fakeBooking('b0', status: 'Completed')]);
    final cubit = AppointmentDetailCubit(repo, 'b0')..load();
    await settled(cubit);
    cubit.reviewed();

    repo
      ..listFailure = const NetworkFailure('offline')
      ..byIdFailure = const NetworkFailure('offline');
    await cubit.load();
    expect(cubit.state.status, AppointmentDetailStatus.error);
    repo
      ..listFailure = null
      ..byIdFailure = null;
    await cubit.load();

    expect(cubit.state.canWriteReview, isFalse);
    await cubit.close();
  });

  test('the review action follows the booking\'s canReview', () async {
    final repo = FakeBookings(past: [fakeBooking('b0', status: 'Completed', canReview: false)]);
    final cubit = AppointmentDetailCubit(repo, 'b0')..load();

    final state = await settled(cubit);

    expect(state.canWriteReview, isFalse);
    await cubit.close();
  });
}
