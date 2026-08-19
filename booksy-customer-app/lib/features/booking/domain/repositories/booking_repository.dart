import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/booking_entities.dart';

/// A booking-creation failure caused by the slot no longer being available
/// (409 from the backend). The UI returns the user to the slot step with
/// refreshed availability.
class SlotTakenFailure extends Failure {
  const SlotTakenFailure(super.message);
}

abstract class BookingRepository {
  Future<Either<Failure, ProviderDetail>> getProviderDetail(String providerId);

  /// [serviceIds] lists every service bundled into one visit (e.g. cut +
  /// colour). The returned slots span their *combined* duration, so a
  /// two-service visit yields fewer, longer slots than either service alone.
  /// [serviceId] stays required because the backend contract still marks it so;
  /// pass the first of [serviceIds]. Omit [serviceIds] for a single service.
  Future<Either<Failure, List<TimeSlot>>> getAvailableSlots({
    required String providerId,
    required String serviceId,
    required DateTime date,
    String? staffId,
    List<String>? serviceIds,
  });

  /// Creates the booking and returns the new booking id.
  ///
  /// See [getAvailableSlots] for the [serviceId]/[serviceIds] relationship: the
  /// booking's duration and price are the sums over [serviceIds] when given.
  Future<Either<Failure, String>> createBooking({
    required String providerId,
    required String serviceId,
    required String staffProviderId,
    required DateTime startTime,
    List<String>? serviceIds,
  });
}
