import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/errors/dio_failure_mapper.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/storage/secure_storage_service.dart';
import '../../domain/entities/booking_summary.dart';
import '../../domain/repositories/bookings_repository.dart';
import '../booking_summary_json.dart';
import '../datasources/bookings_remote_datasource.dart';

class BookingsRepositoryImpl implements BookingsRepository {
  final BookingsRemoteDataSource remoteDataSource;
  final SecureStorageService storageService;

  /// The clock that decides whether a booking is still ahead (injectable
  /// for tests).
  final DateTime Function() now;

  BookingsRepositoryImpl({
    required this.remoteDataSource,
    required this.storageService,
    DateTime Function()? now,
  }) : now = now ?? DateTime.now;

  @override
  Future<Either<Failure, List<BookingSummary>>> getMyBookings({
    required bool upcoming,
    int pageSize = 50,
  }) async {
    try {
      final items = await remoteDataSource.getMyBookings(
        upcoming: upcoming,
        pageSize: pageSize,
      );
      final at = now();
      return Right([
        for (final item in items) BookingSummaryJson.fromListItem(item, now: at),
      ]);
    } on DioException catch (e) {
      return Left(mapDioFailure(e));
    } catch (e) {
      return const Left(ServerFailure(AppStrings.genericError));
    }
  }

  @override
  Future<Either<Failure, BookingSummary>> getBookingById(
    String bookingId,
  ) async {
    try {
      final json = await remoteDataSource.getBookingById(bookingId);
      return Right(BookingSummaryJson.fromDetails(json, now: now()));
    } on DioException catch (e) {
      return Left(mapDioFailure(e));
    } catch (e) {
      return const Left(ServerFailure(AppStrings.genericError));
    }
  }

  @override
  Future<Either<Failure, Unit>> cancelBooking({
    required String bookingId,
    required String reason,
  }) async {
    try {
      final customerId = await storageService.getCustomerId();
      await remoteDataSource.cancelBooking(
        bookingId: bookingId,
        reason: reason,
        cancelledBy: customerId ?? '',
      );
      return const Right(unit);
    } on DioException catch (e) {
      return Left(mapDioFailure(e));
    } catch (e) {
      return const Left(ServerFailure(AppStrings.genericError));
    }
  }

  @override
  Future<Either<Failure, String?>> rescheduleBooking({
    required String bookingId,
    required DateTime newStartTime,
    String? newStaffId,
  }) async {
    try {
      return Right(await remoteDataSource.rescheduleBooking(
        bookingId: bookingId,
        newStartTime: newStartTime,
        newStaffId: newStaffId,
      ));
    } on DioException catch (e) {
      return Left(mapDioFailure(e));
    } catch (e) {
      return const Left(ServerFailure(AppStrings.genericError));
    }
  }
}
