import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart' show visibleForTesting;
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
    List<String>? serviceIds,
  }) async {
    try {
      final json = await remoteDataSource.getAvailableSlots(
        providerId: providerId,
        serviceId: serviceId,
        date: date,
        staffId: staffId,
        serviceIds: serviceIds,
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
    List<String>? serviceIds,
  }) async {
    try {
      final id = await remoteDataSource.createBooking(
        providerId: providerId,
        serviceId: serviceId,
        staffProviderId: staffProviderId,
        startTime: startTime,
        serviceIds: serviceIds,
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
      businessHours: parseBusinessHours(json['businessHours']),
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
      staff: parseStaff(json['staff']),
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

  // ==================== Business hours ====================
  //
  // The server sends each day as:
  //   {"dayOfWeek": 6, "isOpen": true, "openTimeHours": 9, "openTimeMinutes": 0,
  //    "closeTimeHours": 18, "closeTimeMinutes": 30, "breaks": []}
  //
  // The previous parser missed this on three counts at once, which is why the profile rendered a column of
  // bare numbers against empty dashes:
  //   * `dayOfWeek` is an integer, and `.toString()` turned it into "6" instead of a weekday name;
  //   * the times are split across `openTimeHours`/`openTimeMinutes`, so reading `openTime` always gave null
  //     and every row showed " – ";
  //   * the flag is `isOpen`, not `isClosed`, so it defaulted to false and closed days were drawn as if open.

  /// Weekday names indexed by .NET's `DayOfWeek` (0 = Sunday … 6 = Saturday).
  static const List<String> _persianDayNames = [
    'یکشنبه', // 0
    'دوشنبه', // 1
    'سه‌شنبه', // 2
    'چهارشنبه', // 3
    'پنجشنبه', // 4
    'جمعه', // 5
    'شنبه', // 6
  ];

  /// Position of each `DayOfWeek` value in the Iranian week, which starts on Saturday.
  /// Listing days in the server's arbitrary order read as unsorted noise.
  static const List<int> _iranianWeekPosition = [1, 2, 3, 4, 5, 6, 0];

  /// The team members a customer may choose between when booking.
  ///
  /// The backend sends this list already filtered to bookable members, so anyone
  /// here can be selected and will have availability.
  @visibleForTesting
  static List<StaffMember> parseStaff(dynamic raw) {
    if (raw is! List) return const [];

    return raw.whereType<Map<String, dynamic>>().map((s) {
      // fullName wins: a member invited by phone who has not claimed their
      // account yet has no first/last name — only the salon-provided display
      // name — so joining the parts produced an empty label and the picker
      // showed a blank, unidentifiable row.
      final fullName = (s['fullName'] as String?)?.trim();
      final joined = [s['firstName'], s['lastName']]
          .whereType<String>()
          .map((p) => p.trim())
          .where((p) => p.isNotEmpty)
          .join(' ');

      return StaffMember(
        id: s['id'].toString(),
        name: (fullName != null && fullName.isNotEmpty) ? fullName : joined,
        role: s['role'] as String?,
        isActive: s['isActive'] as bool? ?? true,
      );
    }).toList();
  }

  @visibleForTesting
  static List<BusinessHour> parseBusinessHours(dynamic raw) {
    if (raw is! List) return const [];

    final entries = raw.whereType<Map<String, dynamic>>().toList()
      ..sort((a, b) {
        final pa = _weekPosition(a['dayOfWeek']);
        final pb = _weekPosition(b['dayOfWeek']);
        return pa.compareTo(pb);
      });

    return entries.map((h) {
      // `isOpen` is the field actually sent; `isClosed` is honoured only as a fallback for older payloads.
      final isOpen = h['isOpen'] as bool? ?? !(h['isClosed'] as bool? ?? false);

      return BusinessHour(
        dayOfWeek: _dayName(h['dayOfWeek']),
        openTime: _time(h['openTimeHours'], h['openTimeMinutes']),
        closeTime: _time(h['closeTimeHours'], h['closeTimeMinutes']),
        isClosed: !isOpen,
      );
    }).toList();
  }

  static int _weekPosition(dynamic dayOfWeek) {
    final index = (dayOfWeek as num?)?.toInt();
    if (index == null || index < 0 || index > 6) return 99; // unknown days sort last
    return _iranianWeekPosition[index];
  }

  static String _dayName(dynamic dayOfWeek) {
    final index = (dayOfWeek as num?)?.toInt();
    if (index != null && index >= 0 && index <= 6) return _persianDayNames[index];
    // Some payloads may already carry a name; showing it beats showing nothing.
    return dayOfWeek?.toString() ?? '';
  }

  /// `9, 0` becomes `09:00`. Null when the server omitted the time.
  static String? _time(dynamic hours, dynamic minutes) {
    final h = (hours as num?)?.toInt();
    if (h == null) return null;
    final m = (minutes as num?)?.toInt() ?? 0;
    return '${h.toString().padLeft(2, '0')}:${m.toString().padLeft(2, '0')}';
  }
}
