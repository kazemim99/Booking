import 'dart:async';

import 'package:dartz/dartz.dart';

import 'package:asan_rezerve_customer_app/core/errors/failures.dart';
import 'package:asan_rezerve_customer_app/features/booking/domain/entities/booking_entities.dart';
import 'package:asan_rezerve_customer_app/features/booking/domain/repositories/booking_repository.dart';
import 'package:asan_rezerve_customer_app/features/bookings/domain/entities/booking_summary.dart';
import 'package:asan_rezerve_customer_app/features/bookings/domain/repositories/bookings_repository.dart';
import 'package:asan_rezerve_customer_app/features/reviews/domain/entities/review.dart';
import 'package:asan_rezerve_customer_app/features/booking/domain/entities/promotion_entities.dart';

/// A booking as the appointments screens see it. [start] defaults to two days
/// after a fixed "now" so nothing depends on the wall clock.
BookingSummary fakeBooking(
  String id, {
  String status = 'Confirmed',
  DateTime? start,
  bool? actionable,
  bool? canReview,
  String providerId = 'p1',
  String serviceId = 's1',
  String? staffId,
  String? staffName,
  String? rescheduleBlockedReason,
  String? reviewBlockedReason,
  ReviewModerationStatus? reviewStatus,
  bool reviewEditable = false,
  String? reviewBookingId,
  double discountAmount = 0,
  String? discountTitle,
}) {
  final active = actionable ?? const {'confirmed', 'pending', 'requested'}.contains(status.toLowerCase());
  return BookingSummary(
    id: id,
    providerId: providerId,
    providerName: 'سالن نمونه',
    serviceId: serviceId,
    serviceName: 'کوتاهی مو',
    staffId: staffId,
    staffName: staffName,
    startTime: start ?? DateTime(2026, 9, 25, 16, 30),
    durationMinutes: 45,
    price: 250000,
    currency: 'تومان',
    status: status,
    canCancel: active,
    canReschedule: active,
    canReview: canReview ?? (status.toLowerCase() == 'completed' && reviewStatus == null),
    rescheduleBlockedReason: rescheduleBlockedReason,
    reviewBlockedReason: reviewBlockedReason,
    reviewId: reviewStatus == null ? null : 'r-$id',
    reviewStatus: reviewStatus,
    reviewEditable: reviewEditable,
    reviewBookingId: reviewBookingId,
    discountAmount: discountAmount,
    discountTitle: discountTitle,
  );
}

/// In-memory [BookingsRepository]: the lists and the by-id store are the
/// server's state, and a successful cancel/reschedule changes that state the
/// way the server would.
class FakeBookings implements BookingsRepository {
  List<BookingSummary> upcoming;
  List<BookingSummary> past;

  /// Bookings only reachable by id (older than both lists' first page).
  Map<String, BookingSummary> onlyById;

  Failure? listFailure;
  Failure? byIdFailure;
  Failure? cancelFailure;
  Failure? rescheduleFailure;

  int listCalls = 0;
  final byIdCalls = <String>[];
  final cancelCalls = <String>[];

  FakeBookings({
    this.upcoming = const [],
    this.past = const [],
    this.onlyById = const {},
  });

  BookingSummary? _find(String id) => [...upcoming, ...past, ...onlyById.values].where((b) => b.id == id).firstOrNull;

  void _replace(BookingSummary updated) {
    List<BookingSummary> swap(List<BookingSummary> list) => [for (final b in list) b.id == updated.id ? updated : b];
    upcoming = swap(upcoming);
    past = swap(past);
    if (onlyById.containsKey(updated.id)) {
      onlyById = {...onlyById, updated.id: updated};
    }
  }

  @override
  Future<Either<Failure, List<BookingSummary>>> getMyBookings({
    required bool upcoming,
    int pageSize = 50,
  }) async {
    listCalls++;
    if (listFailure != null) return Left(listFailure!);
    return Right(upcoming ? this.upcoming : past);
  }

  @override
  Future<Either<Failure, BookingSummary>> getBookingById(String bookingId) async {
    byIdCalls.add(bookingId);
    if (byIdFailure != null) return Left(byIdFailure!);
    final found = _find(bookingId);
    return found == null ? const Left(ServerFailure('not found')) : Right(found);
  }

  @override
  Future<Either<Failure, Unit>> cancelBooking({
    required String bookingId,
    required String reason,
  }) async {
    cancelCalls.add(bookingId);
    if (cancelFailure != null) return Left(cancelFailure!);
    final found = _find(bookingId);
    if (found != null) {
      _replace(found.copyWith(status: 'Cancelled', canCancel: false, canReschedule: false));
    }
    return const Right(unit);
  }

  /// False plays an older server whose answer does not name the new booking.
  bool namesNewBooking = true;

  @override
  Future<Either<Failure, String?>> rescheduleBooking({
    required String bookingId,
    required DateTime newStartTime,
    String? newStaffId,
  }) async {
    if (rescheduleFailure != null) return Left(rescheduleFailure!);
    final found = _find(bookingId);
    if (found == null) return const Right(null);
    // As the server does: the old booking is closed as Rescheduled, and a new one holds the new time, Requested
    // again for the salon to confirm.
    final newId = '$bookingId-moved';
    _replace(found.copyWith(status: 'Rescheduled', canCancel: false, canReschedule: false));
    final moved = found.copyWith(id: newId, startTime: newStartTime, status: 'Requested');
    if (upcoming.any((b) => b.id == bookingId)) {
      upcoming = [...upcoming, moved];
    } else {
      onlyById = {...onlyById, newId: moved};
    }
    return Right(namesNewBooking ? newId : null);
  }
}

/// [BookingRepository] for the reschedule screen: a provider detail and one
/// answer for every day.
class FakeSlots implements BookingRepository {
  Either<Failure, ProviderDetail> provider;
  DaySlots day;
  final slotRequests = <DateTime>[];

  /// When set, the provider detail arrives only once this completes (a slow profile).
  Completer<void>? providerGate;

  FakeSlots({
    int maxAdvanceBookingDays = 7,
    List<BusinessHour> businessHours = const [],
    this.day = const DaySlots(),
    Failure? providerFailure,
  }) : provider = providerFailure != null
            ? Left(providerFailure)
            : Right(ProviderDetail(
                id: 'p1',
                businessName: 'سالن نمونه',
                averageRating: 0,
                totalReviews: 0,
                maxAdvanceBookingDays: maxAdvanceBookingDays,
                businessHours: businessHours,
                services: const [],
                staff: const [],
              ));

  @override
  Future<Either<Failure, ProviderDetail>> getProviderDetail(String providerId) async {
    await providerGate?.future;
    return provider;
  }

  @override
  Future<Either<Failure, DaySlots>> getAvailableSlots({
    required String providerId,
    required String serviceId,
    required DateTime date,
    String? staffId,
    List<String>? serviceIds,
  }) async {
    slotRequests.add(date);
    return Right(day);
  }

  @override
  Future<List<PublicOffer>> getOffers(String providerId) async => const [];

  @override
  Future<Either<Failure, PriceQuote>> quote({
    required String providerId,
    required List<String> serviceIds,
    required DateTime startTime,
    String? promotionCode,
  }) async =>
      const Left(ServerFailure('no quote in this test'));

  @override
  Future<Either<Failure, String>> createBooking({
    required String providerId,
    required String serviceId,
    required String staffProviderId,
    required DateTime startTime,
    List<String>? serviceIds,
      String? promotionCode,
  }) async =>
      throw UnimplementedError('reschedule never creates a booking');
}
