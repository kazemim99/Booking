import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/booking_summary.dart';

abstract class BookingsRepository {
  /// Upcoming (future, ascending) or past (descending) bookings.
  Future<Either<Failure, List<BookingSummary>>> getMyBookings({
    required bool upcoming,
    int pageSize,
  });

  /// One booking by id — for a booking missing from both lists (older than
  /// the newest of either).
  Future<Either<Failure, BookingSummary>> getBookingById(String bookingId);

  Future<Either<Failure, Unit>> cancelBooking({
    required String bookingId,
    required String reason,
  });

  /// Moves a booking. The server closes it (`Rescheduled`) and opens a new one
  /// in `Requested`; the answer is that new booking's id, or null when the
  /// server did not name it.
  Future<Either<Failure, String?>> rescheduleBooking({
    required String bookingId,
    required DateTime newStartTime,
    String? newStaffId,
  });
}
