import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/booking_summary.dart';

abstract class BookingsRepository {
  /// Upcoming (future, ascending) or past (descending) bookings.
  Future<Either<Failure, List<BookingSummary>>> getMyBookings({
    required bool upcoming,
    int pageSize,
  });

  Future<Either<Failure, Unit>> cancelBooking({
    required String bookingId,
    required String reason,
  });

  Future<Either<Failure, Unit>> rescheduleBooking({
    required String bookingId,
    required DateTime newStartTime,
    String? newStaffId,
  });
}
