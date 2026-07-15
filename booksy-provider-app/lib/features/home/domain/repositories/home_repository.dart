import 'package:dartz/dartz.dart';

import '../../../../core/errors/failures.dart';
import '../entities/home_snapshot.dart';

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
}
