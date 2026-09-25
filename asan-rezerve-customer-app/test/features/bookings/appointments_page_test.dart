import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import 'package:asan_rezerve_customer_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_customer_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_customer_app/core/di/injection.dart';
import 'package:asan_rezerve_customer_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:asan_rezerve_customer_app/features/bookings/presentation/bloc/appointments_bloc.dart';
import 'package:asan_rezerve_customer_app/features/bookings/presentation/pages/appointments_page.dart';
import 'package:asan_rezerve_customer_app/features/bookings/presentation/pages/reschedule_page.dart';
import 'package:asan_rezerve_customer_app/features/booking/domain/repositories/booking_repository.dart';
import 'package:asan_rezerve_customer_app/features/bookings/domain/repositories/bookings_repository.dart';
import 'package:asan_rezerve_customer_app/core/errors/failures.dart';
import 'package:asan_rezerve_customer_app/features/reviews/domain/entities/review.dart';
import 'package:asan_rezerve_customer_app/features/reviews/domain/repositories/review_repository.dart';
import 'package:dartz/dartz.dart';

import '../../helpers/fake_auth_bloc.dart';
import 'bookings_fakes.dart';

/// The appointments tab (UX review 2026-09-23): the Past tab has its own empty text (E.3), a completed visit can
/// be booked again from its card (E.4), and coming back from a booking's detail shows what changed there (E.1).

late FakeBookings _bookings;
late GoRouter _router;
late _FakeReviews _reviews;

class _FakeReviews implements ReviewRepository {
  final created = <String>[];
  Failure? failure;

  @override
  Future<Either<Failure, void>> createReview({
    required String bookingId,
    required double rating,
    String? comment,
    Map<ReviewDimension, double> dimensions = const {},
  }) async {
    if (failure case final f?) return Left(f);
    created.add(bookingId);
    return const Right(null);
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => throw UnimplementedError();
}

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

/// The real app's shape: the tabs are branches of a stateful shell, so the appointments tab stays alive while
/// another tab is shown, and a notification pushes a booking's detail from whichever tab is open.
Widget _shellApp() {
  final auth = FakeAuthBloc()..signIn();
  _router = GoRouter(
    initialLocation: '/appointments',
    routes: [
      StatefulShellRoute.indexedStack(
        builder: (context, state, shell) => Scaffold(body: shell, bottomNavigationBar: const Text('tab-bar')),
        branches: [
          StatefulShellBranch(routes: [
            GoRoute(path: '/home', builder: (context, state) => const Scaffold(body: Text('home'))),
          ]),
          StatefulShellBranch(routes: [
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
          ]),
        ],
      ),
      // Booking covers the tabs (it is drawn above the shell), as in the app.
      GoRoute(
        path: '/providers/:id/book',
        builder: (context, state) => Scaffold(body: Text('book ${state.pathParameters['id']}')),
      ),
    ],
  );

  return BlocProvider<AuthBloc>.value(
    value: auth,
    child: MaterialApp.router(
      theme: AppTheme.light,
      routerConfig: _router,
      builder: (context, child) => Directionality(textDirection: TextDirection.rtl, child: child!),
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
    _reviews = _FakeReviews();
    getIt
      ..registerFactory<AppointmentsBloc>(() => AppointmentsBloc(_bookings))
      ..registerSingleton<ReviewRepository>(_reviews);
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

  group('a booking inside its reschedule window', () {
    const reason = 'تغییر زمان تا 24 ساعت پیش از نوبت ممکن است؛ برای تغییر با سالن تماس بگیرید.';

    testWidgets('shows «تغییر زمان» disabled with the reason, not a dead end at the end', (tester) async {
      _bookings.upcoming = [fakeBooking('b1', rescheduleBlockedReason: reason)];
      await _open(tester);

      expect(find.text(reason), findsOneWidget);
      final button = tester.widget<TextButton>(find.widgetWithText(TextButton, AppStrings.rescheduleBooking));
      expect(button.onPressed, isNull);
    });

    testWidgets('a booking that can be moved has no reason and an enabled button', (tester) async {
      _bookings.upcoming = [fakeBooking('b1')];
      await _open(tester);

      final button = tester.widget<TextButton>(find.widgetWithText(TextButton, AppStrings.rescheduleBooking));
      expect(button.onPressed, isNotNull);
    });
  });

  // openspec/changes/_inline/customer-reviews-and-nahal-seed: «چرا نمیتونم بعنوان مشتری کامنت بذارم؟» — the review
  // was only behind a completed visit's detail page, and nothing said why a finished visit had none.
  group('reviewing from the past list', () {
    testWidgets('a completed visit\'s card offers «ثبت نظر», and once saved says it is waiting for approval',
        (tester) async {
      _bookings.past = [fakeBooking('b0', status: 'Completed', start: DateTime(2026, 5, 10, 14), actionable: false)];
      await _open(tester);
      await _showPast(tester);

      await tester.tap(find.byKey(const Key('booking-card-review-b0')));
      await _settle(tester);
      await tester.tap(find.byKey(const Key('review-star-5')));
      await tester.pump();
      await tester.tap(find.byKey(const Key('review-submit')));
      await _settle(tester);

      expect(_reviews.created, ['b0']);
      expect(find.byKey(const Key('booking-card-review-b0')), findsNothing);
      expect(find.text(AppStrings.reviewCardStatus(AppStrings.reviewStatusPending)), findsOneWidget);
    });

    testWidgets('a refused review says the server\'s reason and keeps the action', (tester) async {
      _reviews.failure = const ServerFailure('برای این نوبت قبلاً نظر ثبت کرده‌اید.');
      _bookings.past = [fakeBooking('b0', status: 'Completed', start: DateTime(2026, 5, 10, 14), actionable: false)];
      await _open(tester);
      await _showPast(tester);

      await tester.tap(find.byKey(const Key('booking-card-review-b0')));
      await _settle(tester);
      await tester.tap(find.byKey(const Key('review-star-4')));
      await tester.pump();
      await tester.tap(find.byKey(const Key('review-submit')));
      await _settle(tester);

      expect(find.text('برای این نوبت قبلاً نظر ثبت کرده‌اید.'), findsOneWidget);
      expect(find.byKey(const Key('booking-card-review-b0')), findsOneWidget);
    });

    testWidgets('a finished visit the salon has not marked done says why, with no review action', (tester) async {
      const reason = 'پس از اینکه سالن این نوبت را «انجام‌شده» ثبت کند، می‌توانید برایش نظر بنویسید.';
      _bookings.past = [
        fakeBooking('bw', start: DateTime(2026, 5, 10, 14), actionable: false, reviewBlockedReason: reason),
      ];
      await _open(tester);
      await _showPast(tester);

      expect(find.byKey(const Key('booking-card-review-blocked-bw')), findsOneWidget);
      expect(find.text(reason), findsOneWidget);
      expect(find.byKey(const Key('booking-card-review-bw')), findsNothing);
    });

    testWidgets('a reviewed visit shows its review\'s state', (tester) async {
      _bookings.past = [
        fakeBooking('br', status: 'Completed', start: DateTime(2026, 5, 10, 14), actionable: false,
            reviewStatus: ReviewModerationStatus.published),
      ];
      await _open(tester);
      await _showPast(tester);

      expect(find.text(AppStrings.reviewCardStatus(AppStrings.reviewStatusPublished)), findsOneWidget);
      expect(find.byKey(const Key('booking-card-review-br')), findsNothing);
      expect(find.text(AppStrings.bookAgain), findsOneWidget, reason: 'a reviewed visit can still be booked again');
    });

    testWidgets('a card with both actions and a status fits a 360 phone at 1.3x text', (tester) async {
      _bookings.past = [
        fakeBooking('b0', status: 'Completed', start: DateTime(2026, 5, 10, 14), actionable: false),
        fakeBooking('bw', start: DateTime(2026, 5, 9, 14), actionable: false,
            reviewBlockedReason: 'پس از اینکه سالن این نوبت را «انجام‌شده» ثبت کند، می‌توانید برایش نظر بنویسید.'),
      ];
      await _open(tester, textScale: 1.3);
      await _showPast(tester);

      expect(tester.takeException(), isNull);
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

  testWidgets('a booking opened from another tab and changed there is re-read when this tab is shown again',
      (tester) async {
    _bookings.upcoming = [fakeBooking('b1', start: DateTime(2030, 1, 5, 16, 30))];
    tester.view.physicalSize = const Size(360 * 3, 640 * 3);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);
    await tester.pumpWidget(_shellApp());
    await _settle(tester);
    expect(find.text(AppStrings.cancelBooking), findsOneWidget);

    // On the home tab, a notification opens the booking; it is cancelled there and the customer backs out.
    _router.go('/home');
    await _settle(tester);
    _router.push('/appointments/b1');
    await _settle(tester);
    expect(find.text('detail b1'), findsOneWidget);
    _bookings.upcoming = [
      _bookings.upcoming.single.copyWith(status: 'Cancelled', canCancel: false, canReschedule: false),
    ];
    _router.pop();
    await _settle(tester);
    expect(_router.routerDelegate.currentConfiguration.uri.path, '/home');
    final callsBefore = _bookings.listCalls;

    _router.go('/appointments');
    await _settle(tester);

    expect(_bookings.listCalls, greaterThan(callsBefore));
    expect(find.text(AppStrings.cancelBooking), findsNothing);
  });

  testWidgets('a booking a notification pushed over this tab is re-read on the way back', (tester) async {
    _bookings.upcoming = [fakeBooking('b1', start: DateTime(2030, 1, 5, 16, 30))];
    await tester.pumpWidget(_shellApp());
    await _settle(tester);

    _router.push('/appointments/b1');
    await _settle(tester);
    expect(find.text('detail b1'), findsOneWidget);
    _bookings.upcoming = [
      _bookings.upcoming.single.copyWith(status: 'Cancelled', canCancel: false, canReschedule: false),
    ];
    final callsBefore = _bookings.listCalls;
    _router.pop();
    await _settle(tester);

    expect(_bookings.listCalls, greaterThan(callsBefore));
    expect(find.text(AppStrings.cancelBooking), findsNothing);
  });

  // Review of the merged branch: the success screen's «مشاهده نوبت‌ها» goes to this tab, which stays alive in the
  // shell; it must show the booking just made, not the list read before it.
  testWidgets('a booking made since this tab was last shown is on it when the customer comes back', (tester) async {
    await tester.pumpWidget(_shellApp());
    await _settle(tester);
    expect(find.text('کوتاهی مو'), findsNothing);

    _router.go('/home');
    await _settle(tester);
    _router.push('/providers/p1/book');
    await _settle(tester);
    expect(find.text('book p1'), findsOneWidget);
    // Booked there.
    _bookings.upcoming = [fakeBooking('b1', status: 'Requested', start: DateTime(2030, 1, 5, 16, 30))];
    final callsBefore = _bookings.listCalls;

    _router.go('/appointments');
    await _settle(tester);

    expect(_bookings.listCalls, greaterThan(callsBefore));
    expect(find.text('کوتاهی مو'), findsOneWidget);
  });

  // Decision 4 (single-purpose tasks leave the tab shell), applied to rescheduling by the review of the merge.
  testWidgets('reschedule from a card covers the tab bar', (tester) async {
    _bookings.upcoming = [fakeBooking('b1', start: DateTime(2030, 1, 5, 16, 30))];
    getIt
      ..registerSingleton<BookingsRepository>(_bookings)
      ..registerSingleton<BookingRepository>(FakeSlots());
    tester.view.physicalSize = const Size(360 * 3, 640 * 3);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);
    await tester.pumpWidget(_shellApp());
    await _settle(tester);
    expect(find.text('tab-bar'), findsOneWidget);

    await tester.tap(find.text(AppStrings.rescheduleBooking));
    await _settle(tester);

    expect(find.byType(ReschedulePage), findsOneWidget);
    expect(find.text('tab-bar'), findsNothing);
  });

  testWidgets('switching tabs without opening a booking does not re-read the list', (tester) async {
    _bookings.upcoming = [fakeBooking('b1', start: DateTime(2030, 1, 5, 16, 30))];
    await tester.pumpWidget(_shellApp());
    await _settle(tester);
    final callsBefore = _bookings.listCalls;

    _router.go('/home');
    await _settle(tester);
    _router.go('/appointments');
    await _settle(tester);

    expect(_bookings.listCalls, callsBefore);
  });
}
