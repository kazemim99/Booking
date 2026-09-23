import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/di/injection.dart';
import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/core/utils/jalali_formatter.dart';
import 'package:booksy_customer_app/core/widgets/app_button.dart';
import 'package:booksy_customer_app/core/widgets/status_badge.dart';
import 'package:booksy_customer_app/features/booking/domain/entities/booking_entities.dart';
import 'package:booksy_customer_app/features/booking/domain/repositories/booking_repository.dart';
import 'package:booksy_customer_app/features/bookings/domain/entities/booking_summary.dart';
import 'package:booksy_customer_app/features/bookings/domain/repositories/bookings_repository.dart';
import 'package:booksy_customer_app/features/bookings/presentation/pages/appointment_detail_page.dart';
import 'package:booksy_customer_app/features/bookings/presentation/pages/reschedule_page.dart';
import 'package:booksy_customer_app/features/reviews/domain/entities/review.dart';
import 'package:booksy_customer_app/features/reviews/domain/repositories/review_repository.dart';

import 'bookings_fakes.dart';
import 'vazir_font.dart';

/// The appointment detail screen (UX review 2026-09-23): the home "next booking" card and notifications open it,
/// so it carries the same actions as the list cards (E.1), a past visit can be booked again (E.4), and its
/// title, salon button and review action read right (E.6).

class _FakeReviews implements ReviewRepository {
  final created = <String>[];

  @override
  Future<Either<Failure, void>> createReview({
    required String bookingId,
    required double rating,
    String? comment,
    Map<ReviewDimension, double> dimensions = const {},
  }) async {
    created.add(bookingId);
    return const Right(null);
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => throw UnimplementedError();
}

// ---------------------------------------------------------------- harness

late FakeBookings _bookings;
late FakeSlots _slots;
late _FakeReviews _reviews;

Widget _app(String bookingId, {double textScale = 1.0, bool inTabs = false}) {
  final detail = GoRoute(
    path: '/appointments/:id',
    builder: (context, state) => AppointmentDetailPage(bookingId: state.pathParameters['id']!),
  );
  final router = GoRouter(
    initialLocation: '/appointments/$bookingId',
    routes: [
      // In the app the detail is inside the tab shell, under the tab bar.
      if (inTabs)
        ShellRoute(
          builder: (context, state, child) => Scaffold(body: child, bottomNavigationBar: const Text('tab-bar')),
          routes: [detail],
        )
      else
        detail,
      GoRoute(
        path: '/providers/:id',
        builder: (context, state) => Scaffold(body: Text('salon ${state.pathParameters['id']}')),
        routes: [
          GoRoute(
            path: 'book',
            builder: (context, state) => Scaffold(body: Text('book ${state.uri}')),
          ),
        ],
      ),
    ],
  );

  return MaterialApp.router(
    theme: AppTheme.light,
    routerConfig: router,
    builder: (context, child) => MediaQuery(
      data: MediaQuery.of(context).copyWith(textScaler: TextScaler.linear(textScale)),
      child: Directionality(textDirection: TextDirection.rtl, child: child!),
    ),
  );
}

/// Bounded pumps: a spinner's endless animation would keep pumpAndSettle from ever settling.
Future<void> _settle(WidgetTester tester) async {
  for (var i = 0; i < 12; i++) {
    await tester.pump(const Duration(milliseconds: 50));
  }
}

Future<void> _phone(WidgetTester tester, {double width = 360}) async {
  tester.view.physicalSize = Size(width * 3, 640 * 3);
  tester.view.devicePixelRatio = 3;
  addTearDown(tester.view.reset);
}

Future<void> _open(
  WidgetTester tester,
  String id, {
  double textScale = 1.0,
  double width = 360,
  bool inTabs = false,
}) async {
  await _phone(tester, width: width);
  await tester.pumpWidget(_app(id, textScale: textScale, inTabs: inTabs));
  await _settle(tester);
}

Finder _inSheet(String text) => find.descendant(of: find.byType(BottomSheet), matching: find.text(text));

/// The sheet's title and its confirm button both read «لغو نوبت»: tap the button.
Finder _sheetButton(String label) =>
    find.descendant(of: find.byType(BottomSheet), matching: find.widgetWithText(AppButton, label));

void main() {
  // The 1.3x checks and the reschedule day strip are measured with the real font.
  setUpAll(loadVazir);

  setUp(() {
    _bookings = FakeBookings();
    _slots = FakeSlots();
    _reviews = _FakeReviews();
    getIt
      ..registerSingleton<BookingsRepository>(_bookings)
      ..registerSingleton<BookingRepository>(_slots)
      ..registerSingleton<ReviewRepository>(_reviews);
  });

  tearDown(() => getIt.reset());

  final upcoming = fakeBooking('b1', start: DateTime(2030, 1, 5, 16, 30));
  final completed = fakeBooking('b0', status: 'Completed', start: DateTime(2026, 5, 10, 14), actionable: false);

  group('titles', () {
    testWidgets('the app bar names the screen, and the salon button says where it goes', (tester) async {
      _bookings.upcoming = [upcoming];
      await _open(tester, 'b1');

      expect(find.descendant(of: find.byType(AppBar), matching: find.text(AppStrings.appointmentDetailTitle)),
          findsOneWidget);
      expect(find.text(AppStrings.appointmentsTitle), findsNothing);

      await tester.tap(find.text(AppStrings.appointmentViewSalon));
      await _settle(tester);
      expect(find.text('salon p1'), findsOneWidget);
    });
  });

  group('an upcoming booking', () {
    testWidgets('offers cancel and reschedule', (tester) async {
      _bookings.upcoming = [upcoming];
      await _open(tester, 'b1');

      expect(find.byKey(const Key('appointment-cancel')), findsOneWidget);
      expect(find.byKey(const Key('appointment-reschedule')), findsOneWidget);
      expect(find.byKey(const Key('appointment-book-again')), findsNothing);
    });

    testWidgets('cancel asks first, then says so and shows the booking cancelled', (tester) async {
      _bookings.upcoming = [upcoming];
      await _open(tester, 'b1');

      await tester.tap(find.byKey(const Key('appointment-cancel')));
      await _settle(tester);
      expect(_inSheet(AppStrings.cancelBookingConfirmBody), findsOneWidget);

      await tester.tap(_sheetButton(AppStrings.cancelBooking));
      await _settle(tester);

      expect(_bookings.cancelCalls, ['b1']);
      expect(find.text(AppStrings.cancelBookingSuccess), findsOneWidget);
      expect(find.text(const StatusBadge(status: BookingStatus.cancelled).label), findsOneWidget);
      expect(find.byKey(const Key('appointment-cancel')), findsNothing);
      expect(find.byKey(const Key('appointment-reschedule')), findsNothing);
    });

    testWidgets('backing out of the sheet cancels nothing', (tester) async {
      _bookings.upcoming = [upcoming];
      await _open(tester, 'b1');

      await tester.tap(find.byKey(const Key('appointment-cancel')));
      await _settle(tester);
      await tester.tap(_sheetButton(AppStrings.back));
      await _settle(tester);

      expect(_bookings.cancelCalls, isEmpty);
      expect(find.byKey(const Key('appointment-cancel')), findsOneWidget);
    });

    testWidgets('a failed cancel says why and keeps the actions', (tester) async {
      _bookings
        ..upcoming = [upcoming]
        ..cancelFailure = const ServerFailure('لغو ممکن نشد');
      await _open(tester, 'b1');

      await tester.tap(find.byKey(const Key('appointment-cancel')));
      await _settle(tester);
      await tester.tap(_sheetButton(AppStrings.cancelBooking));
      await _settle(tester);

      expect(find.text('لغو ممکن نشد'), findsOneWidget);
      expect(find.byKey(const Key('appointment-cancel')), findsOneWidget);
    });

    testWidgets('reschedule opens the slot picker and the detail shows the new time', (tester) async {
      final newStart = DateTime(2030, 1, 6, 11, 15);
      _bookings.upcoming = [upcoming];
      _slots.day = DaySlots(slots: [
        TimeSlot(
          startTime: newStart,
          endTime: newStart.add(const Duration(minutes: 45)),
          durationMinutes: 45,
          isAvailable: true,
        ),
      ]);
      await _open(tester, 'b1');

      await tester.tap(find.byKey(const Key('appointment-reschedule')));
      await _settle(tester);
      expect(find.byType(ReschedulePage), findsOneWidget);

      await tester.tap(find.text(JalaliFormatter.formatTime(newStart)));
      await _settle(tester);
      await tester.tap(find.byKey(const Key('reschedule-submit')));
      await _settle(tester);

      expect(find.byType(ReschedulePage), findsNothing);
      expect(find.text(JalaliFormatter.formatTime(newStart)), findsOneWidget);
      expect(find.text(JalaliFormatter.formatDate(newStart)), findsOneWidget);
    });

    // Decision 4 (single-purpose tasks leave the tab shell), applied to rescheduling by the review of the merge.
    testWidgets('reschedule covers the tab bar', (tester) async {
      _bookings.upcoming = [upcoming];
      await _open(tester, 'b1', inTabs: true);
      expect(find.text('tab-bar'), findsOneWidget);

      await tester.tap(find.byKey(const Key('appointment-reschedule')));
      await _settle(tester);

      expect(find.byType(ReschedulePage), findsOneWidget);
      expect(find.text('tab-bar'), findsNothing);
    });
  });

  group('a past visit', () {
    testWidgets('a completed one can be booked again, with its service chosen', (tester) async {
      _bookings.past = [completed];
      await _open(tester, 'b0');

      expect(find.byKey(const Key('appointment-cancel')), findsNothing);
      expect(find.byKey(const Key('appointment-reschedule')), findsNothing);

      await tester.tap(find.text(AppStrings.bookAgain));
      await _settle(tester);

      expect(find.text('book /providers/p1/book?service=s1'), findsOneWidget);
    });

    testWidgets('a cancelled one offers neither actions nor book-again', (tester) async {
      _bookings.past = [fakeBooking('bc', status: 'Cancelled', start: DateTime(2026, 5, 10, 14))];
      await _open(tester, 'bc');

      expect(find.byKey(const Key('appointment-cancel')), findsNothing);
      expect(find.byKey(const Key('appointment-reschedule')), findsNothing);
      expect(find.byKey(const Key('appointment-book-again')), findsNothing);
      expect(find.byKey(const Key('appointment-write-review')), findsNothing);
    });

    testWidgets('the review action follows canReview', (tester) async {
      _bookings.past = [fakeBooking('b0', status: 'Completed', canReview: false, actionable: false)];
      await _open(tester, 'b0');

      expect(find.byKey(const Key('appointment-write-review')), findsNothing);
    });

    testWidgets('after a review is saved, the review action goes away', (tester) async {
      _bookings.past = [completed];
      // A 412-wide phone: the review dialog's own star row (five 48-dp buttons, reviews feature) does not fit
      // the 232-dp dialog body of a 360-wide one. That is outside this screen; see the change's open issues.
      await _open(tester, 'b0', width: 412);

      await tester.tap(find.byKey(const Key('appointment-write-review')));
      await _settle(tester);
      await tester.tap(find.byKey(const Key('review-star-5')));
      await tester.pump();
      await tester.tap(find.byKey(const Key('review-submit')));
      await _settle(tester);

      expect(_reviews.created, ['b0']);
      expect(find.byKey(const Key('appointment-write-review')), findsNothing);
    });
  });

  testWidgets('a booking older than both lists still opens', (tester) async {
    _bookings.onlyById = {'b-old': completed.copyWithId('b-old')};
    await _open(tester, 'b-old');

    expect(find.text('کوتاهی مو'), findsWidgets);
    expect(find.byKey(const Key('appointment-book-again')), findsOneWidget);
  });

  testWidgets('every action fits a 360x640 phone at 1.3x text', (tester) async {
    _bookings.upcoming = [upcoming];
    await _open(tester, 'b1', textScale: 1.3);
    expect(tester.takeException(), isNull);
  });

  testWidgets('a past visit\'s actions fit a 360x640 phone at 1.3x text', (tester) async {
    _bookings.past = [completed];
    await _open(tester, 'b0', textScale: 1.3);
    expect(tester.takeException(), isNull);
  });
}

extension on BookingSummary {
  BookingSummary copyWithId(String id) => BookingSummary(
        id: id,
        providerId: providerId,
        providerName: providerName,
        serviceId: serviceId,
        serviceName: serviceName,
        staffId: staffId,
        startTime: startTime,
        durationMinutes: durationMinutes,
        price: price,
        currency: currency,
        status: status,
        canCancel: canCancel,
        canReschedule: canReschedule,
        canReview: canReview,
      );
}
