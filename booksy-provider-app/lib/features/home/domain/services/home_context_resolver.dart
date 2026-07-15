import '../../../auth/domain/entities/provider_status.dart';
import '../entities/home_context.dart';
import '../entities/home_enums.dart';
import '../entities/home_inputs.dart';

/// Pure, deterministic resolver: `HomeInputs -> HomeContext` (spec §3).
///
/// No I/O and no clock reads — time-derived values are passed in via inputs —
/// so the same inputs always yield the same context, and every scenario is a
/// single assertion (see `home_context_resolver_test.dart`, the spec §7 matrix).
class HomeContextResolver {
  const HomeContextResolver();

  HomeContext resolve(HomeInputs input) {
    // ---- Layer 0: System (highest precedence) ----
    final SystemState system;
    if (input.loadStatus == DataLoadStatus.failed && !input.hasCachedData) {
      system = SystemState.error;
    } else if (input.connectivity == ConnectivityStatus.offline) {
      system = SystemState.offline; // cached data still surfaced
    } else if (input.loadStatus == DataLoadStatus.loading &&
        !input.hasCachedData) {
      system = SystemState.loading;
    } else {
      system = SystemState.ok;
    }

    // ---- Layer 1: Lifecycle · Config · Availability ----
    final pending =
        input.providerStatus == ProviderStatus.pendingVerification;
    final availability = input.availability; // consumed as-is (backend-managed)

    // ---- Layer 2: Maturity · Day-Context ----
    final maturity = classifyMaturity(input.signals, input.thresholds);

    final HomeDayContext day;
    if (availability != HomeAvailability.open) {
      day = HomeDayContext.none;
    } else if (input.openCapacity == 0 && input.todayApptCount > 0) {
      day = HomeDayContext.fullyBooked;
    } else if (input.todayApptCount > 0) {
      day = HomeDayContext.active;
    } else {
      day = HomeDayContext.noAppts;
    }

    final banners = orderBanners(
      system: system,
      availability: availability,
      pending: pending,
      maturity: maturity,
      hasNudge: input.hasNudge,
    );

    final isStale = system == SystemState.offline ||
        (input.loadStatus == DataLoadStatus.failed && input.hasCachedData);

    return HomeContext(
      system: system,
      pendingVerification: pending,
      bookingMode: input.bookingMode,
      availability: availability,
      maturity: maturity,
      day: day,
      banners: banners,
      isStale: isStale,
      pendingRequestCount: input.pendingRequestCount,
      exceptionCount: input.exceptionCount,
      alertCount: input.alertCount,
      todayApptCount: input.todayApptCount,
      allCompleted: input.allCompleted,
      hasUpcomingToday: input.hasUpcomingToday,
      hasNudge: input.hasNudge,
      completenessPct: input.completenessPct,
      todayBookings: input.todayBookings,
      tomorrowApptCount: input.tomorrowApptCount,
    );
  }

  /// Maturity from traction + completeness signals only (spec §4). Deliberately
  /// independent of [ProviderStatus] — verification is the orthogonal banner.
  static HomeMaturity classifyMaturity(
    MaturitySignals s,
    MaturityThresholds t,
  ) {
    if (s.totalBookingsAllTime == 0) {
      return s.profileComplete ? HomeMaturity.growth : HomeMaturity.setup;
    }
    if (s.bookingsTrailing30d >= t.operationalMinTrailing30d) {
      return HomeMaturity.operational;
    }
    return HomeMaturity.traction;
  }

  /// Fixed severity order (spec §5): error → offline → vacation → closed →
  /// pending → nudge.
  static List<HomeBannerKind> orderBanners({
    required SystemState system,
    required HomeAvailability availability,
    required bool pending,
    required HomeMaturity maturity,
    required bool hasNudge,
  }) {
    final out = <HomeBannerKind>[];
    if (system == SystemState.error) out.add(HomeBannerKind.error);
    if (system == SystemState.offline) out.add(HomeBannerKind.offline);
    if (availability == HomeAvailability.vacation) {
      out.add(HomeBannerKind.vacation);
    }
    if (availability == HomeAvailability.closedToday) {
      out.add(HomeBannerKind.closedToday);
    }
    if (pending) out.add(HomeBannerKind.pending);
    final maturePhase = maturity == HomeMaturity.traction ||
        maturity == HomeMaturity.operational;
    if (maturePhase && hasNudge) out.add(HomeBannerKind.nudge);
    return out;
  }
}
