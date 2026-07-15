import 'package:equatable/equatable.dart';

import '../../../auth/domain/entities/provider_status.dart';
import 'home_booking.dart';
import 'home_enums.dart';

/// Device connectivity, as seen by the Home.
enum ConnectivityStatus { online, offline }

/// Aggregate load status of the Home's data sources.
enum DataLoadStatus { loading, loaded, failed }

/// Maturity thresholds — supplied by backend / remote config (resolved decision
/// #3); never hardcoded. [fallback] is a conservative default used only when
/// remote config is unavailable.
class MaturityThresholds extends Equatable {
  /// Trailing-30-day booking count at/above which a provider is Operational.
  final int operationalMinTrailing30d;

  /// Minimum gallery image count counted toward profile completeness.
  final int galleryMin;

  const MaturityThresholds({
    required this.operationalMinTrailing30d,
    required this.galleryMin,
  });

  static const MaturityThresholds fallback = MaturityThresholds(
    operationalMinTrailing30d: 8,
    galleryMin: 1,
  );

  @override
  List<Object?> get props => [operationalMinTrailing30d, galleryMin];
}

/// Observable signals that classify maturity (spec §4). Deliberately decoupled
/// from [ProviderStatus] — verification is the orthogonal lifecycle layer.
class MaturitySignals extends Equatable {
  final bool profileComplete;
  final int totalBookingsAllTime;
  final int bookingsTrailing30d;

  const MaturitySignals({
    required this.profileComplete,
    required this.totalBookingsAllTime,
    required this.bookingsTrailing30d,
  });

  @override
  List<Object?> get props =>
      [profileComplete, totalBookingsAllTime, bookingsTrailing30d];
}

/// Raw inputs to `HomeContextResolver.resolve` (spec §2).
///
/// Today's booking data is expressed as counts/flags rather than full booking
/// DTOs: the composition engine only needs to decide *which* widgets appear and
/// in *what order*. Each widget fetches its own rich data via its repository —
/// the resolver stays pure and free of the booking data layer.
class HomeInputs extends Equatable {
  // ---- System ----
  final ConnectivityStatus connectivity;
  final DataLoadStatus loadStatus;
  final bool hasCachedData;

  // ---- Lifecycle · Config · Availability ----
  final ProviderStatus providerStatus;
  final HomeBookingMode bookingMode;
  final HomeAvailability availability;

  // ---- Maturity ----
  final MaturitySignals signals;
  final MaturityThresholds thresholds;

  // ---- Today ----
  final int todayApptCount;
  final int openCapacity;
  final bool allCompleted;
  final bool hasUpcomingToday;
  final int pendingRequestCount;
  final int exceptionCount;
  final int alertCount;
  final bool hasNudge;
  final int completenessPct;

  /// Today's booking rows (agenda/now-next/queue render from these).
  final List<HomeBooking> todayBookings;

  /// Tomorrow's booking count (coming-up peek).
  final int tomorrowApptCount;

  const HomeInputs({
    required this.connectivity,
    required this.loadStatus,
    required this.hasCachedData,
    required this.providerStatus,
    required this.bookingMode,
    required this.availability,
    required this.signals,
    this.thresholds = MaturityThresholds.fallback,
    this.todayApptCount = 0,
    this.openCapacity = 0,
    this.allCompleted = false,
    this.hasUpcomingToday = false,
    this.pendingRequestCount = 0,
    this.exceptionCount = 0,
    this.alertCount = 0,
    this.hasNudge = false,
    this.completenessPct = 0,
    this.todayBookings = const [],
    this.tomorrowApptCount = 0,
  });

  @override
  List<Object?> get props => [
        connectivity,
        loadStatus,
        hasCachedData,
        providerStatus,
        bookingMode,
        availability,
        signals,
        thresholds,
        todayApptCount,
        openCapacity,
        allCompleted,
        hasUpcomingToday,
        pendingRequestCount,
        exceptionCount,
        alertCount,
        hasNudge,
        completenessPct,
        todayBookings,
        tomorrowApptCount,
      ];
}
