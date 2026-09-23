import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter/semantics.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import 'package:booksy_customer_app/config/routes/app_router.dart';
import 'package:booksy_customer_app/config/theme/app_colors.dart';
import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/core/storage/secure_storage_service.dart';
import 'package:booksy_customer_app/core/utils/jalali_formatter.dart';
import 'package:booksy_customer_app/core/widgets/widgets.dart';
import 'package:booksy_customer_app/features/auth/domain/entities/user.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_state.dart';
import 'package:booksy_customer_app/features/booking/domain/entities/booking_entities.dart';
import 'package:booksy_customer_app/features/booking/domain/repositories/booking_repository.dart';
import 'package:booksy_customer_app/features/booking/presentation/bloc/booking_bloc.dart';
import 'package:booksy_customer_app/features/booking/presentation/pages/booking_flow_page.dart';
import 'package:booksy_customer_app/features/booking/presentation/widgets/slot_picker.dart';
import 'package:booksy_customer_app/features/profile/data/datasources/profile_remote_datasource.dart';
import 'package:booksy_customer_app/features/profile/presentation/bloc/profile_cubit.dart';

import '../../helpers/fake_auth_bloc.dart';

/// The booking wizard as the customer sees it (UX review 2026-09-23, #4, #9, #17): it can open on a service tapped on
/// the salon's profile, it opens on a day that has free times and says why, the step bar is readable on the blue app
/// bar, the confirm step says what happens next, and the success screen recaps the booking and says the salon still
/// has to accept it.

// A Wednesday. Thursday the 24th, Friday the 25th, …
final _now = DateTime(2026, 9, 23, 10, 30);
DateTime _day(int d) => DateTime(2026, 9, d);

TimeSlot _at(int d, int hour) => TimeSlot(
      startTime: DateTime(2026, 9, d, hour),
      endTime: DateTime(2026, 9, d, hour, 45),
      durationMinutes: 45,
      isAvailable: true,
      staffId: 'st1',
      staffName: 'مریم احمدی',
    );

const _cut = ServiceItem(
  id: 's1',
  name: 'کوتاهی مو',
  price: 250000,
  currency: 'تومان',
  durationMinutes: 45,
);

const _colour = ServiceItem(
  id: 's2',
  name: 'رنگ مو',
  price: 500000,
  currency: 'تومان',
  durationMinutes: 90,
);

const _staff = StaffMember(id: 'st1', name: 'مریم احمدی', isActive: true);

class _Repo implements BookingRepository {
  final Map<int, DaySlots> slotsByDay;
  final List<BusinessHour> hours;
  var createCalls = 0;

  _Repo({this.slotsByDay = const {}, this.hours = const []});

  @override
  Future<Either<Failure, ProviderDetail>> getProviderDetail(String id) async =>
      Right(ProviderDetail(
        id: 'p1',
        businessName: 'سالن نمونه',
        averageRating: 4.8,
        totalReviews: 12,
        maxAdvanceBookingDays: 7,
        businessHours: hours,
        services: const [_cut, _colour],
        staff: const [_staff],
      ));

  @override
  Future<Either<Failure, DaySlots>> getAvailableSlots({
    required String providerId,
    required String serviceId,
    required DateTime date,
    String? staffId,
    List<String>? serviceIds,
  }) async =>
      Right(slotsByDay[date.day] ?? const DaySlots());

  @override
  Future<Either<Failure, String>> createBooking({
    required String providerId,
    required String serviceId,
    required String staffProviderId,
    required DateTime startTime,
    List<String>? serviceIds,
  }) async {
    createCalls++;
    return const Right('b1');
  }
}

/// The customer's profile on the wire: records the name it was sent.
class _Names implements ProfileRemoteDataSource {
  String? firstName;
  String? lastName;
  var calls = 0;

  /// When set, the save fails with this.
  Object? failWith;

  @override
  Future<void> updateProfile({
    required String customerId,
    required String firstName,
    required String lastName,
  }) async {
    calls++;
    if (failWith != null) throw failWith!;
    this.firstName = firstName;
    this.lastName = lastName;
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

/// Enough of the storage for the profile cubit: it reads the customer id before saving.
class _Storage implements SecureStorageService {
  @override
  Future<String?> getCustomerId() async => 'c1';

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

Future<void> _settle(WidgetTester tester) async {
  for (var i = 0; i < 10; i++) {
    await tester.pump(const Duration(milliseconds: 20));
  }
}

/// WCAG relative-luminance contrast ratio.
double _contrast(Color a, Color b) {
  final la = a.computeLuminance();
  final lb = b.computeLuminance();
  final hi = la > lb ? la : lb;
  final lo = la > lb ? lb : la;
  return (hi + 0.05) / (lo + 0.05);
}

/// Flattens a translucent colour onto [background].
Color _over(Color top, Color background) => Color.alphaBlend(top, background);

void main() {
  late GoRouter router;

  Future<BookingBloc> pumpFlow(
    WidgetTester tester, {
    _Repo? repo,
    String? serviceId,
    Size size = const Size(390, 844),
    BookingBloc? existing,
    bool settle = true,
    AuthBloc? auth,
    ProfileCubit? profileCubit,
  }) async {
    tester.view.physicalSize = size * 3;
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final bloc = existing ?? BookingBloc(repo ?? _Repo(), now: () => _now);
    // Closed in a tear-down: `await bloc.close()` inside testWidgets waits on the fake clock.
    addTearDown(bloc.close);

    router = GoRouter(
      initialLocation: Routes.bookingFlow('p1', serviceId: serviceId),
      routes: [
        GoRoute(path: Routes.home, builder: (_, __) => const Text('home-page')),
        GoRoute(
          path: Routes.login,
          builder: (_, __) => const Scaffold(body: Text('login-page')),
        ),
        GoRoute(
          path: Routes.appointments,
          builder: (_, __) => const Text('appointments-page'),
        ),
        GoRoute(
          path: '/providers/:id/book',
          builder: (context, state) => BookingFlowPage(
            providerId: state.pathParameters['id']!,
            initialServiceId: state.uri.queryParameters['service'],
            bloc: bloc,
            profileCubit: profileCubit,
          ),
        ),
      ],
    );
    addTearDown(router.dispose);

    final session = auth ?? FakeAuthBloc();
    if (auth == null) addTearDown(session.close);
    await tester.pumpWidget(BlocProvider<AuthBloc>.value(
      value: session,
      child: MaterialApp.router(
        theme: AppTheme.light,
        routerConfig: router,
        builder: (context, child) =>
            Directionality(textDirection: TextDirection.rtl, child: child!),
      ),
    ));
    if (settle) await _settle(tester);
    return bloc;
  }

  group('a service chosen on the profile', () {
    testWidgets('opens the flow at the time step with that service selected',
        (tester) async {
      final bloc = await pumpFlow(
        tester,
        serviceId: 's2',
        repo: _Repo(slotsByDay: {23: DaySlots(slots: [_at(23, 16)])}),
      );

      expect(bloc.state.services, [_colour]);
      expect(find.text(AppStrings.bookingSelectTime), findsOneWidget);
      expect(find.byType(SlotPicker), findsOneWidget);
    });

    testWidgets('without one the flow starts on the service step',
        (tester) async {
      await pumpFlow(tester);

      expect(find.text(AppStrings.bookingSelectServices), findsOneWidget);
    });
  });

  group('the time step', () {
    testWidgets(
        'an empty today opens on the first day with free times and says why',
        (tester) async {
      final bloc = await pumpFlow(
        tester,
        serviceId: 's1',
        repo: _Repo(slotsByDay: {
          23: const DaySlots(reason: 'مجموعه در این روز تعطیل است.'),
          24: DaySlots(slots: [_at(24, 9), _at(24, 10)]),
        }),
      );

      expect(bloc.state.date, _day(24));
      expect(find.text(AppStrings.bookingMovedFromToday), findsOneWidget);
      expect(find.text(JalaliFormatter.formatTime(_at(24, 9).startTime)),
          findsOneWidget);
      // The strip starts on the injected today, not the device clock.
      expect(tester.widget<SlotPicker>(find.byType(SlotPicker)).today, _day(23));
    });

    testWidgets("the salon's closed weekdays cannot be picked in the strip",
        (tester) async {
      final handle = tester.ensureSemantics();
      await pumpFlow(
        tester,
        serviceId: 's1',
        repo: _Repo(
          slotsByDay: {23: DaySlots(slots: [_at(23, 16)])},
          hours: const [BusinessHour(dayOfWeek: 'پنج‌شنبه', isClosed: true)],
        ),
      );

      final thursday =
          tester.getSemantics(find.byKey(const ValueKey('slot-picker-day-2026-9-24')));
      expect(thursday.label, contains(AppStrings.bookingDayClosed));
      expect(thursday.getSemanticsData().hasAction(SemanticsAction.tap), isFalse);
      handle.dispose();
    });

    testWidgets('an empty day the customer picked offers the next free day',
        (tester) async {
      final bloc = await pumpFlow(
        tester,
        serviceId: 's1',
        repo: _Repo(slotsByDay: {
          23: DaySlots(slots: [_at(23, 16)]),
          28: DaySlots(slots: [_at(28, 12)]),
        }),
      );

      final saturday = find.byKey(const ValueKey('slot-picker-day-2026-9-26'));
      await tester.ensureVisible(saturday);
      await tester.pump();
      await tester.tap(saturday);
      await _settle(tester);
      expect(find.text(AppStrings.bookingNoSlots), findsOneWidget);

      await tester.tap(find.text(AppStrings.bookingFindNextFreeDay));
      await _settle(tester);

      expect(bloc.state.date, _day(28));
      expect(find.text(AppStrings.bookingMovedFromPickedDay), findsOneWidget);
      expect(find.text(JalaliFormatter.formatTime(_at(28, 12).startTime)),
          findsOneWidget);
    });

    testWidgets(
        "no free day in the window shows the salon's reason and no dead-end button",
        (tester) async {
      await pumpFlow(
        tester,
        serviceId: 's1',
        repo: _Repo(slotsByDay: {
          23: const DaySlots(reason: 'مجموعه در این روز تعطیل است.'),
        }),
      );

      expect(find.text('مجموعه در این روز تعطیل است.'), findsOneWidget);
      expect(find.text(AppStrings.bookingNoFreeDayInWindow), findsOneWidget);
      expect(find.text(AppStrings.bookingFindNextFreeDay), findsNothing);
    });
  });

  group('the step bar on the blue app bar', () {
    testWidgets('fill and track are both visible and tell progress apart',
        (tester) async {
      await pumpFlow(tester);

      final bar = tester.widget<LinearProgressIndicator>(
        find.byType(LinearProgressIndicator),
      );
      final fill = bar.valueColor?.value ?? bar.color!;
      final track = bar.backgroundColor!;
      final trackOnBar = _over(track, AppColors.appBar);

      // Before: the fill was the primary blue on the app bar's blue (1.00:1) and the track fell back to the green
      // accent, so the unfilled part read as the progress.
      expect(_contrast(fill, AppColors.appBar), greaterThanOrEqualTo(3));
      expect(_contrast(fill, trackOnBar), greaterThanOrEqualTo(1.5));
      expect(trackOnBar, isNot(AppColors.accent));
      expect(bar.value, closeTo(1 / 3, 0.001),
          reason: 'service → time → confirm with a single staff member');
    });
  });

  group('the confirm step', () {
    Future<BookingBloc> atConfirm(WidgetTester tester, {Size? size}) async {
      final bloc = await pumpFlow(
        tester,
        serviceId: 's1',
        size: size ?? const Size(390, 844),
        repo: _Repo(slotsByDay: {23: DaySlots(slots: [_at(23, 16)])}),
      );
      bloc.add(BookingSlotSelected(_at(23, 16)));
      await _settle(tester);
      return bloc;
    }

    testWidgets('is a compact summary that says what happens next',
        (tester) async {
      await atConfirm(tester);

      final card = find.byType(AppCard);
      expect(card, findsOneWidget);
      // It used to sit in an Expanded and stretch down to the button, mostly empty.
      expect(tester.getSize(card).height, lessThan(844 * 0.6),
          reason: 'the card must not stretch to fill the screen');
      expect(find.ancestor(of: card, matching: find.byType(Scrollable)), findsOneWidget);

      final value = tester.widget<Text>(find.text('سالن نمونه'));
      expect(value.textAlign, TextAlign.end);

      expect(find.text(AppStrings.bookingWhatNextTitle), findsOneWidget);
      expect(find.text(AppStrings.bookingWhatNextBody), findsOneWidget);
      expect(AppStrings.bookingWhatNextBody, contains(AppStrings.statusPending));
      expect(find.text(AppStrings.bookingConfirmCta), findsOneWidget);
    });

    testWidgets('fits a 360x640 screen at 1.3x text', (tester) async {
      tester.platformDispatcher.textScaleFactorTestValue = 1.3;
      addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);

      await atConfirm(tester, size: const Size(360, 640));

      expect(tester.takeException(), isNull);
      await tester.ensureVisible(find.text(AppStrings.bookingConfirmCta));
    });
  });

  // QA recording 2026-09-23 #9: the salon saw «مشتری 9384444636» on the booking. A customer still named by the OTP
  // placeholder gives a first and last name at the confirm step — here there is no skip — and it is saved to their
  // profile and to the session before the booking goes.
  group('a name before the booking goes', () {
    Finder confirm() => find.text(AppStrings.bookingConfirmCta);
    Finder sheet() => find.byKey(const Key('booking-name-sheet'));
    Finder first() => find.byKey(const Key('booking-name-first'));
    Finder last() => find.byKey(const Key('booking-name-last'));
    Finder save() => find.byKey(const Key('booking-name-save'));

    Future<({BookingBloc bloc, _Repo repo, FakeAuthBloc auth, _Names names})> atConfirmAs(
      WidgetTester tester,
      AuthSession? session, {
      Object? saveFails,
      Size size = const Size(390, 844),
    }) async {
      final repo = _Repo(slotsByDay: {23: DaySlots(slots: [_at(23, 16)])});
      final auth = FakeAuthBloc();
      if (session != null) auth.signIn(session);
      addTearDown(auth.close);
      final names = _Names()..failWith = saveFails;
      final cubit = ProfileCubit(remoteDataSource: names, storageService: _Storage());
      addTearDown(cubit.close);

      final bloc = await pumpFlow(
        tester,
        serviceId: 's1',
        size: size,
        repo: repo,
        auth: auth,
        profileCubit: cubit,
      );
      bloc.add(BookingSlotSelected(_at(23, 16)));
      await _settle(tester);
      return (bloc: bloc, repo: repo, auth: auth, names: names);
    }

    String? sessionFirstName(FakeAuthBloc auth) {
      final state = auth.state;
      return state is Authenticated ? state.session.user.firstName : null;
    }

    testWidgets('a customer still named by the OTP placeholder is asked first',
        (tester) async {
      final flow = await atConfirmAs(tester, sessionNamed('مشتری', '9384444636'));

      await tester.tap(confirm());
      await _settle(tester);

      expect(sheet(), findsOneWidget);
      expect(find.text(AppStrings.bookingNameTitle), findsOneWidget);
      expect(first(), findsOneWidget);
      expect(last(), findsOneWidget);
      expect(flow.repo.createCalls, 0, reason: 'nothing is booked yet');
    });

    testWidgets('a session without any name is asked too', (tester) async {
      final flow = await atConfirmAs(tester, sessionNamed(null, null));

      await tester.tap(confirm());
      await _settle(tester);

      expect(sheet(), findsOneWidget);
      expect(flow.repo.createCalls, 0);
    });

    testWidgets('without both names nothing is saved and nothing is booked',
        (tester) async {
      final flow = await atConfirmAs(tester, sessionNamed('مشتری', '9384444636'));
      await tester.tap(confirm());
      await _settle(tester);

      await tester.tap(save());
      await _settle(tester);
      expect(find.text(AppStrings.firstNameRequired), findsOneWidget);
      expect(find.text(AppStrings.lastNameRequired), findsOneWidget);

      await tester.enterText(first(), 'سارا');
      await tester.tap(save());
      await _settle(tester);
      expect(find.text(AppStrings.firstNameRequired), findsNothing);
      expect(find.text(AppStrings.lastNameRequired), findsOneWidget);

      await tester.enterText(first(), '   ');
      await tester.enterText(last(), 'احمدی');
      await tester.tap(save());
      await _settle(tester);
      expect(find.text(AppStrings.firstNameRequired), findsOneWidget);

      expect(flow.names.calls, 0);
      expect(flow.repo.createCalls, 0);
      expect(sheet(), findsOneWidget);
    });

    testWidgets('closing the sheet books nothing and stays on the confirm step',
        (tester) async {
      final flow = await atConfirmAs(tester, sessionNamed('مشتری', '9384444636'));
      await tester.tap(confirm());
      await _settle(tester);

      await tester.tap(find.byKey(const Key('booking-name-cancel')));
      await _settle(tester);

      expect(sheet(), findsNothing);
      expect(flow.repo.createCalls, 0);
      expect(confirm(), findsOneWidget);
    });

    testWidgets(
        'both names are saved to the profile and the session, then the booking goes',
        (tester) async {
      final flow = await atConfirmAs(tester, sessionNamed('مشتری', '9384444636'));
      await tester.tap(confirm());
      await _settle(tester);

      await tester.enterText(first(), ' سارا ');
      await tester.enterText(last(), 'احمدی');
      await tester.tap(save());
      await _settle(tester);

      expect(flow.names.firstName, 'سارا');
      expect(flow.names.lastName, 'احمدی');
      expect(sessionFirstName(flow.auth), 'سارا',
          reason: 'the session has the name, so the next booking does not ask');
      expect(flow.auth.rememberedNames, [('سارا', 'احمدی')]);
      expect(sheet(), findsNothing);
      expect(flow.repo.createCalls, 1);
      expect(find.text(AppStrings.bookingSuccessAwaiting), findsOneWidget);
    });

    testWidgets('a customer with a real name books straight away',
        (tester) async {
      final flow = await atConfirmAs(tester, sessionNamed('سارا', 'احمدی'));

      await tester.tap(confirm());
      await _settle(tester);

      expect(sheet(), findsNothing);
      expect(flow.names.calls, 0);
      expect(flow.repo.createCalls, 1);
      expect(find.text(AppStrings.bookingSuccessAwaiting), findsOneWidget);
    });

    testWidgets('a failed save keeps the sheet open, says so, and books nothing',
        (tester) async {
      final flow = await atConfirmAs(
        tester,
        sessionNamed('مشتری', '9384444636'),
        saveFails: Exception('offline'),
      );
      await tester.tap(confirm());
      await _settle(tester);

      await tester.enterText(first(), 'سارا');
      await tester.enterText(last(), 'احمدی');
      await tester.tap(save());
      await _settle(tester);

      expect(sheet(), findsOneWidget);
      expect(find.text(AppStrings.completeNameSaveFailed), findsOneWidget);
      // What they typed is still there to try again with.
      expect(find.descendant(of: first(), matching: find.text('سارا')), findsOneWidget);
      expect(find.descendant(of: last(), matching: find.text('احمدی')), findsOneWidget);
      expect(flow.repo.createCalls, 0);
      expect(sessionFirstName(flow.auth), 'مشتری');
      expect(flow.auth.rememberedNames, isEmpty);
    });

    testWidgets(
        'signing in at the confirm step still asks a placeholder customer before booking',
        (tester) async {
      final flow = await atConfirmAs(tester, null);

      await tester.tap(confirm());
      await _settle(tester);
      expect(find.text('login-page'), findsOneWidget);

      // The OTP round-trip: signed in as a customer with no name, and back on the confirm step.
      flow.auth.signIn(sessionNamed('مشتری', '9384444636'));
      router.pop();
      // The login page slides away before the confirm button can be tapped.
      await tester.pump(const Duration(seconds: 1));
      await _settle(tester);
      expect(confirm(), findsOneWidget);

      await tester.tap(confirm());
      await _settle(tester);

      expect(sheet(), findsOneWidget);
      expect(flow.repo.createCalls, 0);
    });

    testWidgets('the sheet fits a 360x640 phone at 1.3x text, with 48 dp targets',
        (tester) async {
      tester.platformDispatcher.textScaleFactorTestValue = 1.3;
      addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);
      await atConfirmAs(
        tester,
        sessionNamed('مشتری', '9384444636'),
        size: const Size(360, 640),
      );

      await tester.tap(confirm());
      await _settle(tester);
      // The save error adds a line; it has to fit too.
      await tester.tap(save());
      await _settle(tester);

      expect(tester.takeException(), isNull);
      for (final target in [first(), last(), save(), find.byKey(const Key('booking-name-cancel'))]) {
        await tester.ensureVisible(target);
        expect(tester.getSize(target).height, greaterThanOrEqualTo(48));
      }
    });
  });

  group('the success screen', () {
    Future<BookingBloc> submitted(WidgetTester tester, {Size? size}) async {
      final bloc = await pumpFlow(
        tester,
        serviceId: 's1',
        size: size ?? const Size(390, 844),
        repo: _Repo(slotsByDay: {23: DaySlots(slots: [_at(23, 16)])}),
      );
      bloc.add(BookingSlotSelected(_at(23, 16)));
      await _settle(tester);
      bloc.add(const BookingSubmitted());
      await _settle(tester);
      return bloc;
    }

    testWidgets('recaps salon, service, date and time and says it awaits the salon',
        (tester) async {
      await submitted(tester);

      expect(find.text('سالن نمونه'), findsOneWidget);
      expect(find.text(_cut.name), findsOneWidget);
      expect(find.text(JalaliFormatter.formatDate(_at(23, 16).startTime)),
          findsOneWidget);
      expect(find.text(JalaliFormatter.formatTime(_at(23, 16).startTime)),
          findsOneWidget);
      expect(find.text(AppStrings.bookingSuccessAwaiting), findsOneWidget);
    });

    // Review of the merged branch: the hero check was the green accent (2.38:1 on white).
    testWidgets('the success check is visible on white (3:1)', (tester) async {
      await submitted(tester);

      final check = tester.widget<Icon>(find.byIcon(Icons.check_circle_outline));
      expect(_contrast(check.color!, AppColors.surface), greaterThanOrEqualTo(3));
    });

    testWidgets('view appointments resets the flow and goes to appointments',
        (tester) async {
      final bloc = await submitted(tester);

      await tester.tap(find.text(AppStrings.bookingViewAppointments));
      await _settle(tester);

      expect(find.text('appointments-page'), findsOneWidget);
      expect(bloc.state.providerId, isNull, reason: 'the flow was reset');
    });

    testWidgets('back goes home', (tester) async {
      await submitted(tester);

      await tester.tap(find.text(AppStrings.back));
      await _settle(tester);

      expect(find.text('home-page'), findsOneWidget);
    });

    testWidgets(
        'a booking finished earlier (left with system back) does not stick on '
        'the next visit', (tester) async {
      // The bloc is app-scoped: a customer who left the success screen with the system back button leaves it in
      // the success state, and the next flow must start fresh rather than recap the old booking.
      final repo = _Repo(slotsByDay: {23: DaySlots(slots: [_at(23, 16)])});
      final bloc = BookingBloc(repo, now: () => _now);
      bloc.add(const BookingStarted('p1', serviceId: 's1'));
      await _settle(tester);
      bloc.add(BookingSlotSelected(_at(23, 16)));
      await _settle(tester);
      bloc.add(const BookingSubmitted());
      await _settle(tester);
      expect(bloc.state.submitStatus, SubmitStatus.success);

      await pumpFlow(tester, existing: bloc, settle: false);

      // Not even for the first frame, before the bloc has handled the new visit.
      expect(find.text(AppStrings.bookingSuccessAwaiting), findsNothing);

      await _settle(tester);
      expect(find.text(AppStrings.bookingSuccessAwaiting), findsNothing);
      expect(find.text(AppStrings.bookingSelectServices), findsOneWidget);
    });

    testWidgets('fits a 360x640 screen at 1.3x text', (tester) async {
      tester.platformDispatcher.textScaleFactorTestValue = 1.3;
      addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);

      await submitted(tester, size: const Size(360, 640));

      expect(tester.takeException(), isNull);
    });
  });
}
