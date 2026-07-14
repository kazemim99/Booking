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

  Future<Either<Failure, List<TimeSlot>>> getAvailableSlots({
    required String providerId,
    required String serviceId,
    required DateTime date,
    String? staffId,
  });

  /// Creates the booking and returns the new booking id.
  Future<Either<Failure, String>> createBooking({
    required String providerId,
    required String serviceId,
    required String staffProviderId,
    required DateTime startTime,
  });
}
