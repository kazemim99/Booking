import 'package:booksy_provider_app/features/auth/domain/entities/provider_status.dart';
import 'package:booksy_provider_app/features/home/domain/composition/home_widget_registry.dart';
import 'package:booksy_provider_app/features/home/domain/entities/home_context.dart';
import 'package:booksy_provider_app/features/home/domain/entities/home_enums.dart';
import 'package:booksy_provider_app/features/home/domain/entities/home_inputs.dart';
import 'package:booksy_provider_app/features/home/domain/services/home_context_resolver.dart';
import 'package:flutter_test/flutter_test.dart';

/// Verifies the resolver + registry against the fixture matrix in
/// `booksy-provider-app/PROVIDER_HOME_RESOLVER_SPEC.md` §7. Each row is one
/// assertion of (resolved context) + (ordered visible widgets).
void main() {
  const resolver = HomeContextResolver();
  const registry = HomeWidgetRegistry();

  // Signal presets.
  const setupSignals = MaturitySignals(
    profileComplete: false,
    totalBookingsAllTime: 0,
    bookingsTrailing30d: 0,
  );
  const growthSignals = MaturitySignals(
    profileComplete: true,
    totalBookingsAllTime: 0,
    bookingsTrailing30d: 0,
  );
  const tractionSignals = MaturitySignals(
    profileComplete: true,
    totalBookingsAllTime: 5,
    bookingsTrailing30d: 3,
  );
  const operationalSignals = MaturitySignals(
    profileComplete: true,
    totalBookingsAllTime: 50,
    bookingsTrailing30d: 12,
  );

  HomeInputs inputs({
    ConnectivityStatus connectivity = ConnectivityStatus.online,
    DataLoadStatus loadStatus = DataLoadStatus.loaded,
    bool hasCachedData = true,
    ProviderStatus providerStatus = ProviderStatus.active,
    HomeBookingMode bookingMode = HomeBookingMode.request,
    HomeAvailability availability = HomeAvailability.open,
    MaturitySignals signals = operationalSignals,
    int todayApptCount = 0,
    int openCapacity = 10,
    bool allCompleted = false,
    bool hasUpcomingToday = false,
    int pendingRequestCount = 0,
    int exceptionCount = 0,
    int alertCount = 0,
    bool hasNudge = false,
  }) {
    return HomeInputs(
      connectivity: connectivity,
      loadStatus: loadStatus,
      hasCachedData: hasCachedData,
      providerStatus: providerStatus,
      bookingMode: bookingMode,
      availability: availability,
      signals: signals,
      todayApptCount: todayApptCount,
      openCapacity: openCapacity,
      allCompleted: allCompleted,
      hasUpcomingToday: hasUpcomingToday,
      pendingRequestCount: pendingRequestCount,
      exceptionCount: exceptionCount,
      alertCount: alertCount,
      hasNudge: hasNudge,
    );
  }

  List<HomeWidgetId> order(HomeContext c) => registry.compose(c);

  group('§7 fixture matrix', () {
    test('1 — Setup first-login (pending)', () {
      final c = resolver.resolve(inputs(
        providerStatus: ProviderStatus.pendingVerification,
        signals: setupSignals,
      ));
      expect(c.system, SystemState.ok);
      expect(c.maturity, HomeMaturity.setup);
      expect(c.day, HomeDayContext.noAppts);
      expect(c.pendingVerification, isTrue);
      expect(c.banners, [HomeBannerKind.pending]);
      expect(order(c), [
        HomeWidgetId.statusBannerRail,
        HomeWidgetId.activationChecklist,
        HomeWidgetId.comingUpPeek,
        HomeWidgetId.todayAgenda,
      ]);
    });

    test('2 — Growth empty (verified)', () {
      final c = resolver.resolve(inputs(signals: growthSignals));
      expect(c.maturity, HomeMaturity.growth);
      expect(c.banners, isEmpty);
      expect(order(c), [
        HomeWidgetId.getDiscovered,
        HomeWidgetId.comingUpPeek,
        HomeWidgetId.todayAgenda,
      ]);
    });

    test('3 — No appointments today (operational)', () {
      final c = resolver.resolve(inputs());
      expect(c.maturity, HomeMaturity.operational);
      expect(c.day, HomeDayContext.noAppts);
      expect(order(c), [
        HomeWidgetId.todayAgenda, // 40, positive-empty
        HomeWidgetId.comingUpPeek, // 45, elevated
      ]);
    });

    test('4 — Pending standalone (operational, active, Q>0)', () {
      final c = resolver.resolve(inputs(
        providerStatus: ProviderStatus.pendingVerification,
        todayApptCount: 4,
        hasUpcomingToday: true,
        pendingRequestCount: 2,
      ));
      expect(c.day, HomeDayContext.active);
      expect(c.banners, [HomeBannerKind.pending]);
      expect(order(c), [
        HomeWidgetId.statusBannerRail,
        HomeWidgetId.actionQueue,
        HomeWidgetId.nowNext,
        HomeWidgetId.todayAgenda,
        HomeWidgetId.comingUpPeek,
      ]);
    });

    test('5 — Active day, REQUEST mode', () {
      final c = resolver.resolve(inputs(
        todayApptCount: 4,
        hasUpcomingToday: true,
        pendingRequestCount: 2,
      ));
      expect(order(c), [
        HomeWidgetId.actionQueue, // 30
        HomeWidgetId.nowNext, // 40
        HomeWidgetId.todayAgenda, // 50
        HomeWidgetId.comingUpPeek, // 70
      ]);
    });

    test('6 — Active day, INSTANT mode (no exceptions)', () {
      final c = resolver.resolve(inputs(
        bookingMode: HomeBookingMode.instant,
        todayApptCount: 4,
        hasUpcomingToday: true,
      ));
      expect(order(c), [
        HomeWidgetId.nowNext, // 25 — primary
        HomeWidgetId.todayAgenda, // 50
        HomeWidgetId.comingUpPeek, // 70
      ]);
    });

    test('7 — Active day, INSTANT mode + conflict', () {
      final c = resolver.resolve(inputs(
        bookingMode: HomeBookingMode.instant,
        todayApptCount: 4,
        hasUpcomingToday: true,
        exceptionCount: 1,
      ));
      expect(order(c), [
        HomeWidgetId.nowNext, // 25
        HomeWidgetId.todayAgenda, // 50
        HomeWidgetId.actionQueue, // 55 — demoted, exceptions only
        HomeWidgetId.comingUpPeek, // 70
      ]);
    });

    test('8 — Fully booked (REQUEST)', () {
      final c = resolver.resolve(inputs(
        todayApptCount: 12,
        openCapacity: 0,
        hasUpcomingToday: true,
        pendingRequestCount: 1,
      ));
      expect(c.day, HomeDayContext.fullyBooked);
      expect(order(c), [
        HomeWidgetId.actionQueue,
        HomeWidgetId.nowNext,
        HomeWidgetId.todayAgenda,
        HomeWidgetId.comingUpPeek,
      ]);
    });

    test('9 — End of day (all completed)', () {
      final c = resolver.resolve(inputs(
        todayApptCount: 7,
        allCompleted: true,
        hasUpcomingToday: false,
      ));
      expect(order(c), [
        HomeWidgetId.endOfDaySummary, // 40
        HomeWidgetId.todayAgenda, // 50
        HomeWidgetId.comingUpPeek, // 70
      ]);
    });

    test('10 — Closed today', () {
      final c = resolver.resolve(inputs(
        availability: HomeAvailability.closedToday,
      ));
      expect(c.day, HomeDayContext.none);
      expect(c.banners, [HomeBannerKind.closedToday]);
      expect(order(c), [
        HomeWidgetId.statusBannerRail,
        HomeWidgetId.comingUpPeek, // 45 — next open
        HomeWidgetId.todayAgenda, // 60 — closed-empty
      ]);
    });

    test('11 — Vacation (no in-window appts)', () {
      final c = resolver.resolve(inputs(
        availability: HomeAvailability.vacation,
      ));
      expect(c.banners, [HomeBannerKind.vacation]);
      expect(order(c), [
        HomeWidgetId.statusBannerRail,
        HomeWidgetId.comingUpPeek, // 45
        HomeWidgetId.todayAgenda, // 60
      ]);
    });

    test('12 — Vacation + affected bookings', () {
      final c = resolver.resolve(inputs(
        availability: HomeAvailability.vacation,
        alertCount: 2,
      ));
      expect(order(c), [
        HomeWidgetId.statusBannerRail, // 10
        HomeWidgetId.businessAlerts, // 28 — needs action
        HomeWidgetId.comingUpPeek, // 45
        HomeWidgetId.todayAgenda, // 60
      ]);
    });

    test('13 — Offline + pending (orthogonal; banners stack)', () {
      final c = resolver.resolve(inputs(
        connectivity: ConnectivityStatus.offline,
        providerStatus: ProviderStatus.pendingVerification,
        todayApptCount: 4,
        hasUpcomingToday: true,
      ));
      expect(c.system, SystemState.offline);
      expect(c.isStale, isTrue);
      // Severity order: offline above pending.
      expect(c.banners, [HomeBannerKind.offline, HomeBannerKind.pending]);
      expect(order(c), [
        HomeWidgetId.statusBannerRail,
        HomeWidgetId.nowNext,
        HomeWidgetId.todayAgenda,
        HomeWidgetId.comingUpPeek,
      ]);
    });

    test('14 — Operational + NoAppts + Offline (cached body)', () {
      final c = resolver.resolve(inputs(
        connectivity: ConnectivityStatus.offline,
      ));
      expect(c.system, SystemState.offline);
      expect(c.isStale, isTrue);
      expect(c.banners, [HomeBannerKind.offline]);
      expect(order(c), [
        HomeWidgetId.statusBannerRail,
        HomeWidgetId.todayAgenda, // 40, stale
        HomeWidgetId.comingUpPeek, // 45
      ]);
    });

    test('15 — Total error (no cache) → registry bypassed', () {
      final c = resolver.resolve(inputs(
        loadStatus: DataLoadStatus.failed,
        hasCachedData: false,
      ));
      expect(c.system, SystemState.error);
      expect(order(c), isEmpty);
    });

    test('16 — First load (no cache) → skeleton screen', () {
      final c = resolver.resolve(inputs(
        loadStatus: DataLoadStatus.loading,
        hasCachedData: false,
      ));
      expect(c.system, SystemState.loading);
      expect(order(c), isEmpty);
    });
  });

  group('classifyMaturity (§4)', () {
    const t = MaturityThresholds.fallback; // T = 8

    test('0 bookings + incomplete profile → SETUP', () {
      expect(
        HomeContextResolver.classifyMaturity(setupSignals, t),
        HomeMaturity.setup,
      );
    });

    test('0 bookings + complete profile → GROWTH', () {
      expect(
        HomeContextResolver.classifyMaturity(growthSignals, t),
        HomeMaturity.growth,
      );
    });

    test('≥1 booking + below T trailing → TRACTION', () {
      expect(
        HomeContextResolver.classifyMaturity(tractionSignals, t),
        HomeMaturity.traction,
      );
    });

    test('trailing ≥ T → OPERATIONAL', () {
      expect(
        HomeContextResolver.classifyMaturity(operationalSignals, t),
        HomeMaturity.operational,
      );
    });

    test('boundary: trailing exactly T → OPERATIONAL', () {
      const boundary = MaturitySignals(
        profileComplete: true,
        totalBookingsAllTime: 20,
        bookingsTrailing30d: 8,
      );
      expect(
        HomeContextResolver.classifyMaturity(boundary, t),
        HomeMaturity.operational,
      );
    });

    test('maturity ignores providerStatus (decoupled): pending + complete + '
        '0 bookings resolves GROWTH with a pending banner', () {
      final c = resolver.resolve(inputs(
        providerStatus: ProviderStatus.pendingVerification,
        signals: growthSignals,
      ));
      expect(c.maturity, HomeMaturity.growth);
      expect(c.pendingVerification, isTrue);
      expect(c.banners, contains(HomeBannerKind.pending));
    });
  });

  group('orderBanners (§5)', () {
    test('severity order with several active at once', () {
      final banners = HomeContextResolver.orderBanners(
        system: SystemState.offline,
        availability: HomeAvailability.vacation,
        pending: true,
        maturity: HomeMaturity.operational,
        hasNudge: true,
      );
      expect(banners, [
        HomeBannerKind.offline,
        HomeBannerKind.vacation,
        HomeBannerKind.pending,
        HomeBannerKind.nudge,
      ]);
    });

    test('nudge only surfaces in mature phases', () {
      final setup = HomeContextResolver.orderBanners(
        system: SystemState.ok,
        availability: HomeAvailability.open,
        pending: false,
        maturity: HomeMaturity.setup,
        hasNudge: true,
      );
      expect(setup, isEmpty);
    });
  });
}
