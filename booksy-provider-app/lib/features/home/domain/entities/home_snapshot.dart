import 'package:equatable/equatable.dart';

import '../../../auth/domain/entities/provider_status.dart';
import 'home_booking.dart';
import 'home_enums.dart';
import 'home_inputs.dart';

/// The backend-derived portion of the Home's inputs (resolver spec §2,
/// Layers 1–2). Produced by `HomeRepository.fetchSnapshot`; the system layer
/// (connectivity / load status / cache) is owned by the cubit, which combines
/// the two via [toInputs].
class HomeSnapshot extends Equatable {
  final ProviderStatus providerStatus;
  final HomeBookingMode bookingMode;
  final HomeAvailability availability;
  final MaturitySignals signals;
  final MaturityThresholds thresholds;
  final int todayApptCount;
  final int openCapacity;
  final bool allCompleted;
  final bool hasUpcomingToday;
  final int pendingRequestCount;
  final int exceptionCount;
  final int alertCount;
  final bool hasNudge;
  final int completenessPct;
  final List<HomeBooking> todayBookings;
  final int tomorrowApptCount;

  const HomeSnapshot({
    required this.providerStatus,
    required this.bookingMode,
    required this.availability,
    required this.signals,
    this.thresholds = MaturityThresholds.fallback,
    this.todayApptCount = 0,
    this.openCapacity = 1,
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

  /// Combines this snapshot with the cubit-owned system layer into the full
  /// resolver input.
  HomeInputs toInputs({
    required ConnectivityStatus connectivity,
    required DataLoadStatus loadStatus,
    required bool hasCachedData,
  }) {
    return HomeInputs(
      connectivity: connectivity,
      loadStatus: loadStatus,
      hasCachedData: hasCachedData,
      providerStatus: providerStatus,
      bookingMode: bookingMode,
      availability: availability,
      signals: signals,
      thresholds: thresholds,
      todayApptCount: todayApptCount,
      openCapacity: openCapacity,
      allCompleted: allCompleted,
      hasUpcomingToday: hasUpcomingToday,
      pendingRequestCount: pendingRequestCount,
      exceptionCount: exceptionCount,
      alertCount: alertCount,
      hasNudge: hasNudge,
      completenessPct: completenessPct,
      todayBookings: todayBookings,
      tomorrowApptCount: tomorrowApptCount,
    );
  }

  @override
  List<Object?> get props => [
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
