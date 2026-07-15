import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/core/network/connectivity_service.dart';
import 'package:booksy_provider_app/features/home/domain/entities/home_booking.dart';
import 'package:booksy_provider_app/features/home/domain/repositories/home_repository.dart';
import 'package:booksy_provider_app/features/home/presentation/cubit/calendar_cubit.dart';
import 'package:booksy_provider_app/features/home/presentation/pages/calendar_page.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class MockHomeRepository extends Mock implements HomeRepository {}

class MockConnectivityService extends Mock implements ConnectivityService {}

void main() {
  late MockHomeRepository repository;
  late MockConnectivityService connectivity;

  // Wednesday 2026-07-15; Iranian week starts Saturday 2026-07-11.
  final now = DateTime(2026, 7, 15, 8);

  HomeBooking booking(String id, DateTime start,
          [HomeBookingStatus status = HomeBookingStatus.pending]) =>
      HomeBooking(
        id: id,
        start: start,
        clientName: 'سارا محمدی',
        clientPhone: '0912',
        serviceName: 'اصلاح مو',
        status: status,
      );

  setUp(() {
    repository = MockHomeRepository();
    connectivity = MockConnectivityService();
    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(() => repository.fetchBookings(
          from: any(named: 'from'),
          to: any(named: 'to'),
        )).thenAnswer((_) async => Right([
          booking('b1', DateTime(2026, 7, 15, 10)),
          booking('b2', DateTime(2026, 7, 15, 14),
              HomeBookingStatus.confirmed),
          booking('b3', DateTime(2026, 7, 16, 11),
              HomeBookingStatus.confirmed),
        ]));
    when(() => repository.confirmBooking(any()))
        .thenAnswer((_) async => const Right(null));
  });

  Future<CalendarCubit> pump(WidgetTester tester) async {
    final cubit = CalendarCubit(repository, connectivity, now: () => now);
    addTearDown(cubit.close);
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light, // real theme: infinite-width button footgun
        builder: (context, child) => Directionality(
          textDirection: TextDirection.rtl,
          child: child ?? const SizedBox.shrink(),
        ),
        home: BlocProvider<CalendarCubit>.value(
          value: cubit..load(),
          child: const CalendarView(),
        ),
      ),
    );
    await tester.pumpAndSettle();
    return cubit;
  }

  testWidgets('week strip renders 7 days with count badges; timeline ordered',
      (tester) async {
    await pump(tester);

    // 7 day cells for the week of Sat 11 → Fri 17.
    for (final d in [11, 12, 13, 14, 15, 16, 17]) {
      expect(find.byKey(Key('calendar-day-$d-7')), findsOneWidget);
    }
    // Selected day (today, the 15th) shows both bookings in start order.
    expect(find.byKey(const Key('calendar-booking-b1')), findsOneWidget);
    expect(find.byKey(const Key('calendar-booking-b2')), findsOneWidget);
    final y1 = tester.getTopLeft(find.byKey(const Key('calendar-booking-b1'))).dy;
    final y2 = tester.getTopLeft(find.byKey(const Key('calendar-booking-b2'))).dy;
    expect(y1, lessThan(y2));
  });

  testWidgets('selecting another day switches the timeline without refetch',
      (tester) async {
    await pump(tester);
    clearInteractions(repository);

    await tester.tap(find.byKey(const Key('calendar-day-16-7')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('calendar-booking-b3')), findsOneWidget);
    expect(find.byKey(const Key('calendar-booking-b1')), findsNothing);
    verifyNever(() => repository.fetchBookings(
        from: any(named: 'from'), to: any(named: 'to')));
  });

  testWidgets('pending booking sheet confirms via the cubit', (tester) async {
    await pump(tester);

    await tester.tap(find.byKey(const Key('calendar-booking-b1')));
    await tester.pumpAndSettle();
    expect(find.text(AppStrings.bookingSheetTitle), findsOneWidget);
    expect(find.byKey(const Key('sheet-confirm')), findsOneWidget);
    expect(find.byKey(const Key('sheet-decline')), findsOneWidget);

    await tester.tap(find.byKey(const Key('sheet-confirm')));
    await tester.pumpAndSettle();

    verify(() => repository.confirmBooking('b1')).called(1);
    expect(find.text(AppStrings.homeConfirmed), findsOneWidget); // snackbar
  });

  testWidgets('confirmed booking sheet offers complete/no-show',
      (tester) async {
    await pump(tester);

    await tester.tap(find.byKey(const Key('calendar-booking-b2')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('sheet-complete')), findsOneWidget);
    expect(find.byKey(const Key('sheet-noshow')), findsOneWidget);
    expect(find.byKey(const Key('sheet-confirm')), findsNothing);
  });

  testWidgets('empty day shows the actionable empty state', (tester) async {
    await pump(tester);

    await tester.tap(find.byKey(const Key('calendar-day-17-7')));
    await tester.pumpAndSettle();

    expect(find.text(AppStrings.calendarEmptyDay), findsOneWidget);
    expect(find.text('+ ${AppStrings.homeAddAppointment}'), findsOneWidget);
  });

  testWidgets('total failure shows retry that reloads', (tester) async {
    when(() => repository.fetchBookings(
          from: any(named: 'from'),
          to: any(named: 'to'),
        )).thenAnswer((_) async => const Left(ServerFailure('خطا')));
    await pump(tester);

    expect(find.byKey(const Key('app-error-retry')), findsOneWidget);

    when(() => repository.fetchBookings(
          from: any(named: 'from'),
          to: any(named: 'to'),
        )).thenAnswer((_) async => const Right([]));
    await tester.tap(find.byKey(const Key('app-error-retry')));
    await tester.pumpAndSettle();

    expect(find.text(AppStrings.calendarEmptyDay), findsOneWidget);
  });

  testWidgets('nav bar shows calendar active; today jump present',
      (tester) async {
    await pump(tester);

    // Twice: the app-bar title and the active nav label.
    expect(find.text(AppStrings.navCalendar), findsNWidgets(2));
    expect(find.byKey(const Key('calendar-today')), findsOneWidget);
    expect(find.byKey(const Key('calendar-create-action')), findsOneWidget);
  });
}
