import 'dart:async';

import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/features/booking/domain/entities/booking_entities.dart';
import 'package:booksy_customer_app/features/bookings/presentation/bloc/reschedule_cubit.dart';

import 'bookings_fakes.dart';

/// Rescheduling offers the same days as booking does, and says why a day is empty
/// (UX review 2026-09-23, E.5).
void main() {
  final now = DateTime(2026, 9, 23, 10, 15);

  Future<RescheduleState> settled(RescheduleCubit cubit) async {
    for (var i = 0; i < 10; i++) {
      await Future<void>.value();
    }
    return cubit.state;
  }

  RescheduleCubit cubitOver(FakeSlots slots) => RescheduleCubit(
        bookingRepository: slots,
        bookingsRepository: FakeBookings(upcoming: [fakeBooking('b1')]),
        booking: fakeBooking('b1'),
        now: () => now,
      );

  test('starts on today, from the injected clock', () async {
    final slots = FakeSlots();
    final cubit = cubitOver(slots);

    final state = await settled(cubit);

    expect(state.selectedDate, DateTime(2026, 9, 23));
    expect(slots.slotRequests, [DateTime(2026, 9, 23)]);
    await cubit.close();
  });

  test('the day strip covers the salon\'s booking window', () async {
    final cubit = cubitOver(FakeSlots(maxAdvanceBookingDays: 5));

    final state = await settled(cubit);

    expect(state.maxAdvanceBookingDays, 5);
    await cubit.close();
  });

  test('a day picked before the window arrived that lies outside it goes back to today, with its times', () async {
    final slots = FakeSlots(maxAdvanceBookingDays: 2)..providerGate = Completer<void>();
    final cubit = cubitOver(slots);
    await settled(cubit);

    // The seven-day default strip is still up: the customer taps the sixth day.
    await cubit.loadSlots(DateTime(2026, 9, 28));
    slots.providerGate!.complete();
    final state = await settled(cubit);

    expect(state.maxAdvanceBookingDays, 2);
    expect(state.selectedDate, DateTime(2026, 9, 23));
    expect(slots.slotRequests.last, DateTime(2026, 9, 23));
    expect(state.status, RescheduleStatus.pickingSlots);
    await cubit.close();
  });

  test('a day picked before the window arrived that lies inside it is kept', () async {
    final slots = FakeSlots(maxAdvanceBookingDays: 2)..providerGate = Completer<void>();
    final cubit = cubitOver(slots);
    await settled(cubit);

    // The last day the salon takes: today plus two.
    await cubit.loadSlots(DateTime(2026, 9, 25));
    slots.providerGate!.complete();
    final state = await settled(cubit);

    expect(state.selectedDate, DateTime(2026, 9, 25));
    expect(slots.slotRequests, [DateTime(2026, 9, 23), DateTime(2026, 9, 25)]);
    await cubit.close();
  });

  test('without the salon\'s profile the window is the customer\'s seven days', () async {
    final cubit = cubitOver(FakeSlots(providerFailure: const NetworkFailure('offline')));

    final state = await settled(cubit);

    expect(state.maxAdvanceBookingDays, 7);
    await cubit.close();
  });

  test('an empty day carries the salon\'s reason, and a day with times carries none', () async {
    final slots = FakeSlots(day: const DaySlots(reason: 'مجموعه در این روز تعطیل است.'));
    final cubit = cubitOver(slots);

    expect((await settled(cubit)).slotsReason, 'مجموعه در این روز تعطیل است.');

    slots.day = DaySlots(slots: [
      TimeSlot(
        startTime: DateTime(2026, 9, 24, 10),
        endTime: DateTime(2026, 9, 24, 10, 45),
        durationMinutes: 45,
        isAvailable: true,
      ),
    ]);
    await cubit.loadSlots(DateTime(2026, 9, 24));

    expect(cubit.state.slots, hasLength(1));
    expect(cubit.state.slotsReason, isNull);
    await cubit.close();
  });
}
