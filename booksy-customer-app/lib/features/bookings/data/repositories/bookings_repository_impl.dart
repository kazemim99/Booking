import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/errors/dio_failure_mapper.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/storage/secure_storage_service.dart';
import '../../domain/entities/booking_summary.dart';
import '../../domain/repositories/bookings_repository.dart';
import '../datasources/bookings_remote_datasource.dart';

class BookingsRepositoryImpl implements BookingsRepository {
  final BookingsRemoteDataSource remoteDataSource;
  final SecureStorageService storageService;

  BookingsRepositoryImpl({
    required this.remoteDataSource,
    required this.storageService,
  });

  static const _cancellableStatuses = {'pending', 'requested', 'confirmed'};

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
      return Right(items.map(_parseSummary).toList());
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
  Future<Either<Failure, Unit>> rescheduleBooking({
    required String bookingId,
    required DateTime newStartTime,
    String? newStaffId,
  }) async {
    try {
      await remoteDataSource.rescheduleBooking(
        bookingId: bookingId,
        newStartTime: newStartTime,
        newStaffId: newStaffId,
      );
      return const Right(unit);
    } on DioException catch (e) {
      return Left(mapDioFailure(e));
    } catch (e) {
      return const Left(ServerFailure(AppStrings.genericError));
    }
  }

  /// Maps the backend CustomerBookingDto. Cancel/reschedule eligibility is
  /// derived from what the API allows: an active status and a future start.
  BookingSummary _parseSummary(Map<String, dynamic> json) {
    final startTime =
        DateTime.parse(json['startTime'] as String).toLocal();
    final status = (json['status'] ?? '').toString();
    final actionable = _cancellableStatuses.contains(status.toLowerCase()) &&
        startTime.isAfter(DateTime.now());

    return BookingSummary(
      id: (json['bookingId'] ?? json['id']).toString(),
      providerId: (json['providerId'] ?? '').toString(),
      providerName: json['providerName'] as String? ?? '',
      providerImageUrl: json['providerImageUrl'] as String?,
      serviceId: (json['serviceId'] ?? '').toString(),
      serviceName: json['serviceName'] as String? ?? '',
      staffId: json['staffId']?.toString(),
      startTime: startTime,
      durationMinutes: (json['durationMinutes'] as num?)?.toInt() ?? 0,
      price: (json['totalPrice'] as num?)?.toDouble() ??
          (json['totalAmount'] as num?)?.toDouble() ??
          0,
      currency: json['currency'] as String? ?? '',
      status: status,
      canCancel: actionable,
      canReschedule: actionable,
      canReview: status.toLowerCase() == 'completed',
      cancellationReason: json['cancellationReason'] as String?,
    );
  }
}
