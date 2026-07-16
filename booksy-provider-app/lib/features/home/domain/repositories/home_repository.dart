import 'package:dartz/dartz.dart';

import '../../../../core/errors/failures.dart';
import '../entities/composer_models.dart';
import '../entities/home_booking.dart';
import '../entities/home_snapshot.dart';
import '../entities/provider_client.dart';

/// Supplies the backend-derived Home inputs (resolver spec §2).
///
/// Implementations compose bookings/status/statistics into one [HomeSnapshot]
/// per fetch. Data-source swaps (e.g. a dedicated provider-clients endpoint, a
/// backend availability payload, push-fed updates) happen behind this
/// interface without touching the cubit or UI (resolved decisions #2/#4/#6).
abstract class HomeRepository {
  /// One consistent snapshot of the provider's current Home inputs.
  Future<Either<Failure, HomeSnapshot>> fetchSnapshot();

  // ---- Booking quick actions (Home T0/T1; spec: two-tap promises) ----

  /// Provider approves a pending booking request.
  Future<Either<Failure, void>> confirmBooking(String id);

  /// Provider declines/cancels a booking, with a client-visible [reason].
  Future<Either<Failure, void>> declineBooking(String id,
      {required String reason});

  /// Marks a booking completed.
  Future<Either<Failure, void>> completeBooking(String id);

  /// Marks a booking as a client no-show.
  Future<Either<Failure, void>> markNoShow(String id);

  // ---- Calendar (spec: provider-calendar) ----

  /// The provider's bookings within [from, to), enriched with service names
  /// (same row model the Home consumes).
  Future<Either<Failure, List<HomeBooking>>> fetchBookings({
    required DateTime from,
    required DateTime to,
  });

  // ---- Clients (spec: provider-clients) ----

  /// The provider's client book, most-recent activity first.
  Future<Either<Failure, List<ProviderClient>>> fetchClients();

  // ---- Booking composer (spec: provider-booking-composer) ----

  /// The composer's pickable catalog: the provider's services and staff.
  Future<Either<Failure, ComposerCatalog>> fetchComposerCatalog();

  /// Available start times (local) for the selection on [date].
  Future<Either<Failure, List<DateTime>>> fetchAvailableSlots({
    required String serviceId,
    required DateTime date,
    String? staffId,
  });

  /// Creates a walk-in booking. Client name/phone (when given) are carried in
  /// the notes per the MVP walk-in convention.
  Future<Either<Failure, void>> createBooking({
    required String serviceId,
    required String staffId,
    required DateTime startTime,
    String? clientName,
    String? clientPhone,
    String? notes,
  });
}
