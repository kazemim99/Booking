import 'dart:async';

import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/core/network/connectivity_service.dart';
import 'package:booksy_provider_app/features/home/domain/entities/home_booking.dart';
import 'package:booksy_provider_app/features/home/domain/repositories/home_repository.dart';
import 'package:booksy_provider_app/features/home/presentation/cubit/calendar_cubit.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class MockHomeRepository extends Mock implements HomeRepository {}

class MockConnectivityService extends Mock implements ConnectivityService {}

void main() {
  late MockHomeRepository repository;
  late MockConnectivityService connectivity;

  // Wednesday 2026-07-15 → Iranian week starts Saturday 2026-07-11.
  final now = DateTime(2026, 7, 15, 10);
  final weekStart = DateTime(2026, 7, 11);

  HomeBooking booking(String id, DateTime start,
          [HomeBookingStatus status = HomeBookingStatus.confirmed]) =>
      HomeBooking(
        id: id,
        start: start,
        clientName: 'سارا',
        serviceName: 'اصلاح',
        status: status,
      );

  setUp(() {
    repository = MockHomeRepository();
    connectivity = MockConnectivityService();
    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(() => repository.fetchBookings(
          from: any(named: 'from'),
          to: any(named: 'to'),
        )).thenAnswer((_) async => const Right([]));
  });

  CalendarCubit build() =>
      CalendarCubit(repository, connectivity, now: () => now);

  test('initial window: Saturday week start, today selected', () {
    final cubit = build();
    expect(cubit.state.weekStart, weekStart);
    expect(cubit.state.selectedDay, DateTime(2026, 7, 15));
    cubit.close();
  });

  test('weekStartOf anchors any weekday to its Saturday', () {
    expect(CalendarCubit.weekStartOf(DateTime(2026, 7, 11)), weekStart); // Sat
    expect(CalendarCubit.weekStartOf(DateTime(2026, 7, 12)), weekStart); // Sun
    expect(CalendarCubit.weekStartOf(DateTime(2026, 7, 17)), weekStart); // Fri
    expect(CalendarCubit.weekStartOf(DateTime(2026, 7, 18)),
        DateTime(2026, 7, 18)); // next Sat
  });

  test('load groups bookings by day sorted by start', () async {
    when(() => repository.fetchBookings(
          from: any(named: 'from'),
          to: any(named: 'to'),
        )).thenAnswer((_) async => Right([
          booking('b2', DateTime(2026, 7, 15, 14)),
          booking('b1', DateTime(2026, 7, 15, 9)),
          booking('b3', DateTime(2026, 7, 16, 11)),
        ]));

    final cubit = build();
    await cubit.load();

    expect(cubit.state.status, CalendarStatus.ready);
    expect(cubit.state.selectedDayBookings.map((b) => b.id), ['b1', 'b2']);
    expect(cubit.state.countFor(DateTime(2026, 7, 16)), 1);
    verify(() => repository.fetchBookings(
          from: weekStart,
          to: weekStart.add(const Duration(days: 7)),
        )).called(1);
    await cubit.close();
  });

  test('selectDay is pure state — no refetch', () async {
    final cubit = build();
    await cubit.load();
    clearInteractions(repository);

    cubit.selectDay(DateTime(2026, 7, 16));

    expect(cubit.state.selectedDay, DateTime(2026, 7, 16));
    verifyNever(() => repository.fetchBookings(
        from: any(named: 'from'), to: any(named: 'to')));
    await cubit.close();
  });

  test('fast week paging discards the superseded response', () async {
    final cubit = build();
    await cubit.load();

    final slow = Completer<Either<Failure, List<HomeBooking>>>();
    final nextWeekStart = weekStart.add(const Duration(days: 7));
    final weekAfterStart = weekStart.add(const Duration(days: 14));
    when(() => repository.fetchBookings(
          from: nextWeekStart,
          to: any(named: 'to'),
        )).thenAnswer((_) => slow.future);
    when(() => repository.fetchBookings(
          from: weekAfterStart,
          to: any(named: 'to'),
        )).thenAnswer((_) async => Right([
          booking('w2', weekAfterStart.add(const Duration(hours: 10))),
        ]));

    unawaited(cubit.nextWeek()); // slow, in flight
    await cubit.nextWeek(); // fast, wins
    expect(cubit.state.weekStart, weekAfterStart);
    expect(cubit.state.countFor(weekAfterStart), 1);

    slow.complete(Right([
      booking('stale', nextWeekStart.add(const Duration(hours: 9))),
    ]));
    await Future<void>.delayed(Duration.zero);

    // The stale week's data never overwrites the newer week.
    expect(cubit.state.weekStart, weekAfterStart);
    expect(cubit.state.countFor(weekAfterStart), 1);
    expect(cubit.state.countFor(nextWeekStart), 0);
    await cubit.close();
  });

  test('jumpToToday returns to the current week and selects today', () async {
    final cubit = build();
    await cubit.load();
    await cubit.nextWeek();
    expect(cubit.state.weekStart, isNot(weekStart));

    await cubit.jumpToToday();

    expect(cubit.state.weekStart, weekStart);
    expect(cubit.state.selectedDay, DateTime(2026, 7, 15));
    await cubit.close();
  });

  test('refresh failure with data goes stale, without data goes failed',
      () async {
    when(() => repository.fetchBookings(
          from: any(named: 'from'),
          to: any(named: 'to'),
        )).thenAnswer(
            (_) async => Right([booking('b1', DateTime(2026, 7, 15, 9))]));
    final cubit = build();
    await cubit.load();

    when(() => repository.fetchBookings(
          from: any(named: 'from'),
          to: any(named: 'to'),
        )).thenAnswer((_) async => const Left(ServerFailure('خطا')));
    await cubit.refresh();

    expect(cubit.state.status, CalendarStatus.ready);
    expect(cubit.state.stale, isTrue);
    expect(cubit.state.selectedDayBookings, hasLength(1));

    final fresh = build();
    await fresh.load();
    expect(fresh.state.status, CalendarStatus.failed);
    expect(fresh.state.error, 'خطا');
    await cubit.close();
    await fresh.close();
  });

  test('mutations: offline refused; success refreshes the week', () async {
    final cubit = build();
    await cubit.load();

    when(() => connectivity.isOnline).thenAnswer((_) async => false);
    final offline = await cubit.confirmBooking('b1');
    expect(offline, isA<NetworkFailure>());

    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(() => repository.confirmBooking(any()))
        .thenAnswer((_) async => const Right(null));
    clearInteractions(repository);
    when(() => repository.fetchBookings(
          from: any(named: 'from'),
          to: any(named: 'to'),
        )).thenAnswer((_) async => const Right([]));

    final ok = await cubit.confirmBooking('b1');
    await Future<void>.delayed(Duration.zero);

    expect(ok, isNull);
    verify(() => repository.fetchBookings(
        from: any(named: 'from'), to: any(named: 'to'))).called(1);
    await cubit.close();
  });
}
