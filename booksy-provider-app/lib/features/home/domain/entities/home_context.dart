import 'package:equatable/equatable.dart';

import 'home_booking.dart';
import 'home_enums.dart';

/// The single immutable value object the Home renders from (spec §3).
///
/// Produced by `HomeContextResolver.resolve`. The orchestrator and every
/// widget's visibility rule read ONLY from this — widgets never re-derive
/// global state. Carrying the small set of counts/flags below lets the widget
/// registry decide visibility and priority without touching the data layer.
class HomeContext extends Equatable {
  // ---- Resolved layers ----
  final SystemState system;
  final bool pendingVerification;
  final HomeBookingMode bookingMode;
  final HomeAvailability availability;
  final HomeMaturity maturity;
  final HomeDayContext day;

  /// Banners in resolved severity order (spec §5). The rail renders the top two
  /// expanded and collapses the rest.
  final List<HomeBannerKind> banners;

  /// True when displayed data is cached/stale (offline, or a failed refresh
  /// that fell back to cache) — widgets show a "last updated" affordance.
  final bool isStale;

  // ---- Carried composition inputs (for widget visibility/priority) ----
  final int pendingRequestCount;
  final int exceptionCount;
  final int alertCount;
  final int todayApptCount;
  final bool allCompleted;
  final bool hasUpcomingToday;
  final bool hasNudge;
  final int completenessPct;

  /// Today's booking rows (agenda/now-next/queue render from these).
  final List<HomeBooking> todayBookings;

  /// Tomorrow's booking count (coming-up peek).
  final int tomorrowApptCount;

  const HomeContext({
    required this.system,
    required this.pendingVerification,
    required this.bookingMode,
    required this.availability,
    required this.maturity,
    required this.day,
    required this.banners,
    required this.isStale,
    required this.pendingRequestCount,
    required this.exceptionCount,
    required this.alertCount,
    required this.todayApptCount,
    required this.allCompleted,
    required this.hasUpcomingToday,
    required this.hasNudge,
    required this.completenessPct,
    this.todayBookings = const [],
    this.tomorrowApptCount = 0,
  });

  @override
  List<Object?> get props => [
        system,
        pendingVerification,
        bookingMode,
        availability,
        maturity,
        day,
        banners,
        isStale,
        pendingRequestCount,
        exceptionCount,
        alertCount,
        todayApptCount,
        allCompleted,
        hasUpcomingToday,
        hasNudge,
        completenessPct,
        todayBookings,
        tomorrowApptCount,
      ];
}
