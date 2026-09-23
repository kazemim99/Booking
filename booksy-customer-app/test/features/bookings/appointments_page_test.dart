import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/di/injection.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:booksy_customer_app/features/bookings/presentation/bloc/appointments_bloc.dart';
import 'package:booksy_customer_app/features/bookings/presentation/pages/appointments_page.dart';

import '../../helpers/fake_auth_bloc.dart';
import 'bookings_fakes.dart';

/// The appointments tab (UX review 2026-09-23): the Past tab has its own empty text (E.3), a completed visit can
/// be booked again from its card (E.4), and coming back from a booking's detail shows what changed there (E.1).

late FakeBookings _bookings;
late GoRouter _router;

Widget _app({double textScale = 1.0}) {
  final auth = FakeAuthBloc()..signIn();
  _router = GoRouter(
    initialLocation: '/appointments',
    routes: [
      GoRoute(
        path: '/appointments',
        builder: (context, state) => const AppointmentsPage(),
        routes: [
          GoRoute(
            path: ':id',
            builder: (context, state) => Scaffold(body: Text('detail ${state.pathParameters['id']}')),
          ),
        ],
      ),
      GoRoute(
        path: '/providers/:id/book',
        builder: (context, state) => Scaffold(body: Text('book ${state.uri}')),
      ),
      GoRoute(path: '/explore', builder: (context, state) => const Scaffold(body: Text('explore'))),
    ],
  );

  return BlocProvider<AuthBloc>.value(
    value: auth,
    child: MaterialApp.router(
      theme: AppTheme.light,
      routerConfig: _router,
      builder: (context, child) => MediaQuery(
        data: MediaQuery.of(context).copyWith(textScaler: TextScaler.linear(textScale)),
        child: Directionality(textDirection: TextDirection.rtl, child: child!),
      ),
    ),
  );
}

Future<void> _settle(WidgetTester tester) async {
  for (var i = 0; i < 12; i++) {
    await tester.pump(const Duration(milliseconds: 50));
  }
}

Future<void> _open(WidgetTester tester, {double textScale = 1.0}) async {
  tester.view.physicalSize = const Size(360 * 3, 640 * 3);
  tester.view.devicePixelRatio = 3;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(_app(textScale: textScale));
  await _settle(tester);
}

Future<void> _showPast(WidgetTester tester) async {
  await tester.tap(find.text(AppStrings.appointmentsPast));
  await _settle(tester);
}

void main() {
  setUp(() {
    _bookings = FakeBookings();
    getIt.registerFactory<AppointmentsBloc>(() => AppointmentsBloc(_bookings));
  });

  tearDown(() => getIt.reset());

  group('empty tabs', () {
    testWidgets('the Past tab speaks of past appointments', (tester) async {
      _bookings.upcoming = [fakeBooking('b1', start: DateTime(2030, 1, 5, 16, 30))];
      await _open(tester);
      await _showPast(tester);

      expect(find.text(AppStrings.appointmentsPastEmptyTitle), findsOneWidget);
      expect(find.text(AppStrings.appointmentsPastEmptySubtitle), findsOneWidget);
      expect(find.text(AppStrings.appointmentsEmptyTitle), findsNothing);
    });

    testWidgets('with no bookings at all, each tab keeps its own text', (tester) async {
      await _open(tester);
      expect(find.text(AppStrings.appointmentsEmptyTitle), findsOneWidget);

      await _showPast(tester);
      expect(find.text(AppStrings.appointmentsPastEmptyTitle), findsOneWidget);
      expect(find.text(AppStrings.appointmentsEmptyTitle), findsNothing);
    });
  });

  group('book again', () {
    testWidgets('a completed visit\'s card opens the booking flow with its service chosen', (tester) async {
      _bookings.past = [fakeBooking('b0', status: 'Completed', start: DateTime(2026, 5, 10, 14), actionable: false)];
      await _open(tester);
      await _showPast(tester);

      await tester.tap(find.text(AppStrings.bookAgain));
      await _settle(tester);

      expect(find.text('book /providers/p1/book?service=s1'), findsOneWidget);
    });

    testWidgets('a cancelled visit\'s card does not offer it', (tester) async {
      _bookings.past = [fakeBooking('bc', status: 'Cancelled', start: DateTime(2026, 5, 10, 14))];
      await _open(tester);
      await _showPast(tester);

      expect(find.text(AppStrings.bookAgain), findsNothing);
    });

    testWidgets('the card fits a 360x640 phone at 1.3x text', (tester) async {
      _bookings.past = [fakeBooking('b0', status: 'Completed', start: DateTime(2026, 5, 10, 14), actionable: false)];
      await _open(tester, textScale: 1.3);
      await _showPast(tester);

      expect(tester.takeException(), isNull);
    });
  });

  testWidgets('coming back from a booking\'s detail re-reads the list', (tester) async {
    _bookings.upcoming = [fakeBooking('b1', start: DateTime(2030, 1, 5, 16, 30))];
    await _open(tester);
    final callsBefore = _bookings.listCalls;

    await tester.tap(find.text('کوتاهی مو'));
    await _settle(tester);
    expect(find.text('detail b1'), findsOneWidget);

    // Cancelled there.
    _bookings.upcoming = [
      _bookings.upcoming.single.copyWith(status: 'Cancelled', canCancel: false, canReschedule: false),
    ];
    _router.pop();
    await _settle(tester);

    expect(_bookings.listCalls, greaterThan(callsBefore));
    expect(find.text(AppStrings.cancelBooking), findsNothing);
  });
}
