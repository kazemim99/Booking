import 'package:bloc_test/bloc_test.dart';
import 'package:booksy_provider_app/config/theme/app_theme.dart';
import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_session.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_status.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_event.dart';
import 'package:booksy_provider_app/features/auth/presentation/bloc/auth_state.dart';
import 'package:booksy_provider_app/features/home/domain/entities/home_booking.dart';
import 'package:booksy_provider_app/features/home/domain/entities/home_context.dart';
import 'package:booksy_provider_app/features/home/domain/entities/home_enums.dart';
import 'package:booksy_provider_app/features/home/presentation/cubit/home_cubit.dart';
import 'package:booksy_provider_app/features/home/presentation/pages/home_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockHomeCubit extends MockCubit<HomeContext> implements HomeCubit {}

class _MockAuthBloc extends MockBloc<AuthEvent, AuthState>
    implements AuthBloc {}

/// Builds a [HomeContext] directly (the resolver is unit-tested separately —
/// widget tests drive the view with explicit contexts).
HomeContext ctx({
  SystemState system = SystemState.ok,
  bool pending = false,
  HomeBookingMode mode = HomeBookingMode.request,
  HomeAvailability availability = HomeAvailability.open,
  HomeMaturity maturity = HomeMaturity.operational,
  HomeDayContext day = HomeDayContext.active,
  List<HomeBannerKind> banners = const [],
  bool isStale = false,
  List<HomeBooking> bookings = const [],
  int tomorrow = 0,
  bool allCompleted = false,
  bool hasUpcoming = true,
  int completenessPct = 70,
}) {
  return HomeContext(
    system: system,
    pendingVerification: pending,
    bookingMode: mode,
    availability: availability,
    maturity: maturity,
    day: day,
    banners: banners,
    isStale: isStale,
    pendingRequestCount:
        bookings.where((b) => b.status == HomeBookingStatus.pending).length,
    exceptionCount: 0,
    alertCount: 0,
    todayApptCount: bookings.length,
    allCompleted: allCompleted,
    hasUpcomingToday: hasUpcoming,
    hasNudge: false,
    completenessPct: completenessPct,
    todayBookings: bookings,
    tomorrowApptCount: tomorrow,
  );
}

HomeBooking booking(
  String id, {
  HomeBookingStatus status = HomeBookingStatus.confirmed,
  // Offset from NOW, not a wall-clock hour: a fixed hour flips from future
  // to past as the real day advances, making NowNext's اکنون/بعدی label
  // time-of-day dependent (flaked at 23:00+).
  Duration fromNow = const Duration(hours: 2),
}) {
  return HomeBooking(
    id: id,
    start: DateTime.now().add(fromNow),
    clientName: 'سارا محمدی',
    clientPhone: '09121112233',
    serviceName: 'اصلاح مو',
    status: status,
  );
}

ProviderSession get _session => ProviderSession(
      accessToken: 'a',
      refreshToken: 'r',
      expiresIn: 900,
      user: const ProviderUser(
        id: 'u-1',
        phoneNumber: '09121234567',
        fullName: 'سالن رُز',
      ),
      providerId: 'p-1',
      providerStatus: ProviderStatus.active,
      isNewProvider: false,
      requiresOnboarding: false,
    );

void main() {
  late _MockHomeCubit cubit;
  late _MockAuthBloc authBloc;

  setUp(() {
    cubit = _MockHomeCubit();
    authBloc = _MockAuthBloc();
    whenListen(
      authBloc,
      const Stream<AuthState>.empty(),
      initialState: Authenticated(_session),
    );
    when(() => cubit.refresh()).thenAnswer((_) async {});
    when(() => cubit.confirmBooking(any())).thenAnswer((_) async => null);
    when(() => cubit.declineBooking(any(), reason: any(named: 'reason')))
        .thenAnswer((_) async => null);
    when(() => cubit.completeBooking(any())).thenAnswer((_) async => null);
    when(() => cubit.markNoShow(any())).thenAnswer((_) async => null);
  });

  Future<void> pump(WidgetTester tester, HomeContext state) async {
    whenListen(cubit, const Stream<HomeContext>.empty(), initialState: state);
    await tester.pumpWidget(
      MaterialApp(
        // The REAL theme: guards the infinite-width button footgun — themed
        // buttons inside Rows must be width-constrained or layout throws.
        theme: AppTheme.light,
        builder: (context, child) => Directionality(
          textDirection: TextDirection.rtl,
          child: child ?? const SizedBox.shrink(),
        ),
        home: MultiBlocProvider(
          providers: [
            BlocProvider<HomeCubit>.value(value: cubit),
            BlocProvider<AuthBloc>.value(value: authBloc),
          ],
          child: const HomeView(),
        ),
      ),
    );
  }

  group('system chrome', () {
    testWidgets('LOADING renders the skeleton, no zone list', (tester) async {
      await pump(tester, ctx(system: SystemState.loading));
      expect(find.byKey(const Key('home-skeleton')), findsOneWidget);
      expect(find.byKey(const Key('home-zone-list')), findsNothing);
    });

    testWidgets('ERROR renders centered retry that calls refresh',
        (tester) async {
      await pump(tester, ctx(system: SystemState.error));
      expect(find.text(AppStrings.homeLoadError), findsOneWidget);
      await tester.tap(find.byKey(const Key('app-error-retry')));
      verify(() => cubit.refresh()).called(1);
    });
  });

  group('Setup composition (first login)', () {
    testWidgets('pending banner + checklist hero + inviting empty agenda',
        (tester) async {
      await pump(
        tester,
        ctx(
          pending: true,
          maturity: HomeMaturity.setup,
          day: HomeDayContext.noAppts,
          banners: const [HomeBannerKind.pending],
          hasUpcoming: false,
        ),
      );

      // Banner (priority 10) above the checklist hero (20) — both on screen.
      expect(find.byKey(const Key('home-banner-pending')), findsOneWidget);
      expect(find.text(AppStrings.homeChecklistTitle), findsOneWidget);
      final bannerY =
          tester.getTopLeft(find.byKey(const Key('home-banner-pending'))).dy;
      final checklistY =
          tester.getTopLeft(find.text(AppStrings.homeChecklistTitle)).dy;
      expect(bannerY, lessThan(checklistY));
      // The muted empty agenda sits below the fold (priority 60) — scroll to
      // it (zone ORDER itself is covered by the registry unit tests).
      await tester.scrollUntilVisible(
        find.text(AppStrings.homeAgendaEmptyTitle),
        200,
        scrollable: find.byType(Scrollable).first,
      );
      expect(find.text(AppStrings.homeAgendaEmptyTitle), findsOneWidget);
      // Create action is present.
      expect(find.byKey(const Key('home-create-action')), findsOneWidget);
    });
  });

  group('Growth composition', () {
    testWidgets('GetDiscovered hero with share CTA and completeness',
        (tester) async {
      await pump(
        tester,
        ctx(
          maturity: HomeMaturity.growth,
          day: HomeDayContext.noAppts,
          hasUpcoming: false,
        ),
      );
      expect(find.text(AppStrings.homeDiscoverTitle), findsOneWidget);
      expect(find.byKey(const Key('home-share-link')), findsOneWidget);
      expect(find.text(AppStrings.homeChecklistTitle), findsNothing);
    });
  });

  group('Operational active day', () {
    final bookings = [
      booking('b1', status: HomeBookingStatus.completed, fromNow: const Duration(hours: -2)),
      booking('b2', status: HomeBookingStatus.pending, fromNow: const Duration(hours: 2)),
      booking('b3', fromNow: const Duration(hours: 2)),
    ];

    testWidgets(
        'REQUEST mode: queue above now/next above agenda; real theme lays out '
        'without the infinite-width crash', (tester) async {
      await pump(tester, ctx(bookings: bookings));

      // No layout exception with the real theme (footgun guard).
      expect(tester.takeException(), isNull);

      expect(find.text(AppStrings.homeRequestsTitle), findsOneWidget);
      final queueY =
          tester.getTopLeft(find.text(AppStrings.homeRequestsTitle)).dy;
      final nowNextY = tester.getTopLeft(find.text(AppStrings.homeNextLabel)).dy;
      final agendaY =
          tester.getTopLeft(find.text(AppStrings.homeAgendaTitle)).dy;
      expect(queueY, lessThan(nowNextY));
      expect(nowNextY, lessThan(agendaY));
    });

    testWidgets('INSTANT mode: now/next leads and the queue is absent',
        (tester) async {
      await pump(
        tester,
        ctx(mode: HomeBookingMode.instant, bookings: bookings),
      );
      // Exceptions are zero → the queue hides entirely in instant mode.
      expect(find.text(AppStrings.homeRequestsTitle), findsNothing);
      expect(find.text(AppStrings.homeNextLabel), findsOneWidget);
    });

    testWidgets('confirming a request routes to the cubit and reports success',
        (tester) async {
      await pump(tester, ctx(bookings: bookings));
      await tester.ensureVisible(find.byKey(const Key('confirm-b2')));
      await tester.tap(find.byKey(const Key('confirm-b2')));
      await tester.pump();
      verify(() => cubit.confirmBooking('b2')).called(1);
      await tester.pump();
      expect(find.text(AppStrings.homeConfirmed), findsOneWidget);
    });

    testWidgets('end-of-day summary replaces now/next when all completed',
        (tester) async {
      await pump(
        tester,
        ctx(
          bookings: [
            booking('b1', status: HomeBookingStatus.completed, fromNow: const Duration(hours: -2)),
          ],
          allCompleted: true,
          hasUpcoming: false,
        ),
      );
      expect(find.byKey(const Key('home-end-of-day')), findsOneWidget);
      expect(find.text(AppStrings.homeNextLabel), findsNothing);
    });
  });

  group('offline', () {
    testWidgets('offline banner pins while cached agenda stays visible',
        (tester) async {
      await pump(
        tester,
        ctx(
          system: SystemState.offline,
          banners: const [HomeBannerKind.offline],
          isStale: true,
          bookings: [booking('b1', fromNow: const Duration(hours: 2))],
        ),
      );
      expect(find.byKey(const Key('home-banner-offline')), findsOneWidget);
      expect(find.text(AppStrings.homeAgendaTitle), findsOneWidget);
    });
  });

  group('accessibility & RTL', () {
    testWidgets('operational layout survives 1.3× font scale without '
        'overflow exceptions', (tester) async {
      tester.platformDispatcher.textScaleFactorTestValue = 1.3;
      addTearDown(tester.platformDispatcher.clearAllTestValues);
      await pump(
        tester,
        ctx(bookings: [
          booking('b1', status: HomeBookingStatus.pending, fromNow: const Duration(hours: 2)),
          booking('b2', fromNow: const Duration(hours: 2)),
        ]),
      );
      expect(tester.takeException(), isNull);
    });
  });
}
