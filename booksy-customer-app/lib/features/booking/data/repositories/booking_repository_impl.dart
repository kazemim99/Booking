import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/errors/dio_failure_mapper.dart';
import '../../../../core/errors/failures.dart';
import '../../domain/entities/booking_entities.dart';
import '../../domain/repositories/booking_repository.dart';
import '../datasources/booking_remote_datasource.dart';

class BookingRepositoryImpl implements BookingRepository {
  final BookingRemoteDataSource remoteDataSource;

  BookingRepositoryImpl({required this.remoteDataSource});

  @override
  Future<Either<Failure, ProviderDetail>> getProviderDetail(
    String providerId,
  ) async {
    try {
      final json = await remoteDataSource.getProviderDetail(providerId);
      return Right(_parseProvider(json));
    } on DioException catch (e) {
      if (e.response?.statusCode == 404) {
        return const Left(ServerFailure(AppStrings.providerNotFound));
      }
      return Left(mapDioFailure(e));
    } catch (e) {
      return const Left(ServerFailure(AppStrings.genericError));
    }
  }

  @override
  Future<Either<Failure, List<TimeSlot>>> getAvailableSlots({
    required String providerId,
    required String serviceId,
    required DateTime date,
    String? staffId,
  }) async {
    try {
      final json = await remoteDataSource.getAvailableSlots(
        providerId: providerId,
        serviceId: serviceId,
        date: date,
        staffId: staffId,
      );
      final slots = (json['slots'] as List<dynamic>? ?? [])
          .whereType<Map<String, dynamic>>()
          .map(_parseSlot)
          .where((s) => s.isAvailable)
          .toList();
      return Right(slots);
    } on DioException catch (e) {
      return Left(mapDioFailure(e));
    } catch (e) {
      return const Left(ServerFailure(AppStrings.genericError));
    }
  }

  @override
  Future<Either<Failure, String>> createBooking({
    required String providerId,
    required String serviceId,
    required String staffProviderId,
    required DateTime startTime,
  }) async {
    try {
      final id = await remoteDataSource.createBooking(
        providerId: providerId,
        serviceId: serviceId,
        staffProviderId: staffProviderId,
        startTime: startTime,
      );
      return Right(id);
    } on DioException catch (e) {
      final status = e.response?.statusCode;
      // Conflict: slot got booked between selection and confirmation.
      if (status == 409 || status == 422) {
        return const Left(SlotTakenFailure(AppStrings.bookingSlotTaken));
      }
      return Left(mapDioFailure(e));
    } catch (e) {
      return const Left(ServerFailure(AppStrings.genericError));
    }
  }

  ProviderDetail _parseProvider(Map<String, dynamic> json) {
    final address = json['address'] as Map<String, dynamic>?;
    return ProviderDetail(
      id: json['id'].toString(),
      businessName: json['businessName'] as String? ?? '',
      description: json['description'] as String?,
      city: address?['city'] as String?,
      addressLine: address?['street'] as String? ??
          address?['addressLine1'] as String?,
      logoUrl: json['logoUrl'] as String?,
      profileImageUrl: json['profileImageUrl'] as String?,
      averageRating: (json['averageRating'] as num?)?.toDouble() ?? 0,
      totalReviews: (json['totalReviews'] as num?)?.toInt() ?? 0,
      businessHours: (json['businessHours'] as List<dynamic>? ?? [])
          .whereType<Map<String, dynamic>>()
          .map((h) => BusinessHour(
                dayOfWeek: (h['dayOfWeek'] ?? h['day'] ?? '').toString(),
                openTime: h['openTime']?.toString() ??
                    h['startTime']?.toString(),
                closeTime: h['closeTime']?.toString() ??
                    h['endTime']?.toString(),
                isClosed: h['isClosed'] as bool? ?? false,
              ))
          .toList(),
      services: (json['services'] as List<dynamic>? ?? [])
          .whereType<Map<String, dynamic>>()
          .map((s) => ServiceItem(
                id: s['id'].toString(),
                name: s['name'] as String? ?? '',
                description: s['description'] as String?,
                price: (s['basePrice'] as num?)?.toDouble() ??
                    (s['price'] as num?)?.toDouble() ??
                    0,
                currency: s['currency'] as String? ?? '',
                durationMinutes: (s['duration'] as num?)?.toInt() ??
                    (s['durationMinutes'] as num?)?.toInt() ??
                    0,
                imageUrl: s['imageUrl'] as String?,
              ))
          .toList(),
      staff: (json['staff'] as List<dynamic>? ?? [])
          .whereType<Map<String, dynamic>>()
          .map((s) => StaffMember(
                id: s['id'].toString(),
                name: [s['firstName'], s['lastName']]
                    .whereType<String>()
                    .where((p) => p.isNotEmpty)
                    .join(' '),
                role: s['role'] as String?,
                isActive: s['isActive'] as bool? ?? true,
              ))
          .toList(),
    );
  }

  TimeSlot _parseSlot(Map<String, dynamic> json) {
    return TimeSlot(
      startTime: DateTime.parse(json['startTime'] as String).toLocal(),
      endTime: DateTime.parse(json['endTime'] as String).toLocal(),
      durationMinutes: (json['durationMinutes'] as num?)?.toInt() ?? 0,
      isAvailable: json['isAvailable'] as bool? ?? false,
      staffId: json['availableStaffId']?.toString(),
      staffName: json['availableStaffName'] as String?,
    );
  }
}
