/// Enumerations for the Provider Home ("Today") composition.
///
/// These mirror `booksy-provider-app/PROVIDER_HOME_RESOLVER_SPEC.md` §1 and are
/// the vocabulary the resolver (`HomeContextResolver`) and the widget registry
/// (`HomeWidgetRegistry`) operate on. Kept free of any Flutter/UI dependency so
/// the whole composition engine is pure and unit-testable.
library;

/// System layer — highest precedence (spec §1, Layer 0).
enum SystemState { ok, loading, offline, error }

/// Hybrid booking model (resolved decision #1). Reorders the top operational
/// widgets: Request → the Action Queue leads; Instant → Now/Next + agenda lead.
enum HomeBookingMode { instant, request }

/// Availability is backend-managed (resolved decision #6) — the Home consumes
/// it, it is never computed locally.
enum HomeAvailability { open, closedToday, vacation }

/// Business-maturity phase (spec §4) — a computed property, never a manual
/// setting. Drives whether the scaffold or the agenda leads.
enum HomeMaturity { setup, growth, traction, operational }

/// Day-context — meaningful only when [HomeAvailability.open].
enum HomeDayContext { none, noAppts, active, fullyBooked }

/// Banner kinds, in the fixed severity order used by `orderBanners` (spec §5).
enum HomeBannerKind { error, offline, vacation, closedToday, pending, nudge }

/// The scrollable Home zone widgets the registry orders. The app bar and the
/// center-docked create action are chrome (outside the scroll list) and are
/// intentionally NOT members here.
enum HomeWidgetId {
  statusBannerRail,
  activationChecklist,
  getDiscovered,
  nowNext,
  endOfDaySummary,
  businessAlerts,
  actionQueue,
  todayAgenda,
  comingUpPeek,
  setupNudges,
}
