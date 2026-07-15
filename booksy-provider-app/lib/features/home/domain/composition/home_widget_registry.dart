import '../entities/home_context.dart';
import '../entities/home_enums.dart';

/// The declarative Home widget registry (spec §6).
///
/// Each widget's visibility and priority are pure expressions over
/// [HomeContext]. The orchestrator calls [compose] and renders the returned
/// ids top-to-bottom. Adding / removing / reordering / replacing a widget is a
/// change here — never a change to the Home page. Priorities are named
/// constants with gaps so widgets can be inserted without renumbering; lower =
/// higher on screen.
class HomeWidgetRegistry {
  const HomeWidgetRegistry();

  /// Ordered, visible widgets for [c]. Returns empty for the system states the
  /// orchestrator renders as full-screen chrome (loading skeleton / error).
  List<HomeWidgetId> compose(HomeContext c) {
    if (c.system == SystemState.error || c.system == SystemState.loading) {
      return const [];
    }
    final ids = HomeWidgetId.values.where((id) => isVisible(c, id)).toList()
      ..sort((a, b) => priority(c, a).compareTo(priority(c, b)));
    return ids;
  }

  /// Whether [id] applies to [c].
  bool isVisible(HomeContext c, HomeWidgetId id) {
    final cached = c.system == SystemState.ok || c.system == SystemState.offline;
    final operational =
        c.availability == HomeAvailability.open && _isOperationalDay(c);
    switch (id) {
      case HomeWidgetId.statusBannerRail:
        return c.banners.isNotEmpty;
      case HomeWidgetId.activationChecklist:
        return c.maturity == HomeMaturity.setup;
      case HomeWidgetId.getDiscovered:
        return c.maturity == HomeMaturity.growth;
      case HomeWidgetId.nowNext:
        return operational && c.hasUpcomingToday && !c.allCompleted;
      case HomeWidgetId.endOfDaySummary:
        return operational && c.allCompleted;
      case HomeWidgetId.businessAlerts:
        return c.alertCount > 0;
      case HomeWidgetId.actionQueue:
        return cached &&
            c.availability == HomeAvailability.open &&
            _queueCount(c) > 0;
      case HomeWidgetId.todayAgenda:
        return cached;
      case HomeWidgetId.comingUpPeek:
        return cached;
      case HomeWidgetId.setupNudges:
        final mature = c.maturity == HomeMaturity.traction ||
            c.maturity == HomeMaturity.operational;
        return mature && c.hasNudge;
    }
  }

  /// On-screen order for [id] (lower = higher). Booking-mode reordering lives
  /// here: Request → ActionQueue leads; Instant → NowNext leads.
  int priority(HomeContext c, HomeWidgetId id) {
    final instant = c.bookingMode == HomeBookingMode.instant;
    switch (id) {
      case HomeWidgetId.statusBannerRail:
        return 10;
      case HomeWidgetId.activationChecklist:
        return 20;
      case HomeWidgetId.getDiscovered:
        return 20;
      case HomeWidgetId.nowNext:
        return instant ? 25 : 40;
      case HomeWidgetId.endOfDaySummary:
        return instant ? 25 : 40;
      case HomeWidgetId.businessAlerts:
        return 28;
      case HomeWidgetId.actionQueue:
        return c.bookingMode == HomeBookingMode.request ? 30 : 55;
      case HomeWidgetId.todayAgenda:
        return _agendaPriority(c);
      case HomeWidgetId.comingUpPeek:
        return _peekElevated(c) ? 45 : 70;
      case HomeWidgetId.setupNudges:
        return 80;
    }
  }

  int _queueCount(HomeContext c) => c.bookingMode == HomeBookingMode.request
      ? c.pendingRequestCount
      : c.exceptionCount;

  bool _isOperationalDay(HomeContext c) =>
      c.day == HomeDayContext.active || c.day == HomeDayContext.fullyBooked;

  int _agendaPriority(HomeContext c) {
    if (c.availability != HomeAvailability.open) return 60; // closed/vacation
    if (c.maturity == HomeMaturity.setup ||
        c.maturity == HomeMaturity.growth) {
      return 60; // muted — scaffold is the hero
    }
    if (c.day == HomeDayContext.noAppts) return 40; // positive-empty message
    return 50; // normal timeline
  }

  bool _peekElevated(HomeContext c) =>
      c.day == HomeDayContext.noAppts ||
      c.availability == HomeAvailability.closedToday ||
      c.availability == HomeAvailability.vacation ||
      c.maturity == HomeMaturity.setup ||
      c.maturity == HomeMaturity.growth;
}
