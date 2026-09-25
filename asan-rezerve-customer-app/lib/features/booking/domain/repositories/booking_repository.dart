import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/booking_entities.dart';
import '../entities/promotion_entities.dart';

/// A booking-creation failure caused by the slot no longer being available
/// (409 from the backend). The UI returns the user to the slot step with
/// refreshed availability.
class SlotTakenFailure extends Failure {
  const SlotTakenFailure(super.message);
}

/// The discount the visit was quoted with went away before the booking was saved (the last use was taken, or the
/// salon paused it). The time is still free — only the price changed — so the customer stays on the confirm step
/// and sees the new price (409 `PROMOTION_UNAVAILABLE`).
class PromotionUnavailableFailure extends Failure {
  const PromotionUnavailableFailure(super.message);
}

abstract class BookingRepository {
  Future<Either<Failure, ProviderDetail>> getProviderDetail(String providerId);

  /// [serviceIds] lists every service bundled into one visit (e.g. cut +
  /// colour). The returned slots span their *combined* duration, so a
  /// two-service visit yields fewer, longer slots than either service alone.
  /// [serviceId] stays required because the backend contract still marks it so;
  /// pass the first of [serviceIds]. Omit [serviceIds] for a single service.
  Future<Either<Failure, DaySlots>> getAvailableSlots({
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
    String? promotionCode,
  });

  /// A salon's automatic offers, for badges on its services. Empty when they cannot be read.
  Future<List<PublicOffer>> getOffers(String providerId);

  /// The server's price for the visit: subtotal, the one discount it would apply and what happened to the code.
  Future<Either<Failure, PriceQuote>> quote({
    required String providerId,
    required List<String> serviceIds,
    required DateTime startTime,
    String? promotionCode,
  });
}
