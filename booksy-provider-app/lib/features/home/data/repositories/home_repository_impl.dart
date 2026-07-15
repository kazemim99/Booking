import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';

import '../../../../core/errors/failures.dart';
import '../../../auth/domain/entities/provider_status.dart';
import '../../../auth/domain/repositories/auth_repository.dart';
import '../../domain/entities/home_booking.dart';
import '../../domain/entities/home_enums.dart';
import '../../domain/entities/home_inputs.dart';
import '../../domain/entities/home_snapshot.dart';
import '../../domain/repositories/home_repository.dart';
import '../datasources/home_api_service.dart';

/// Composes the Home snapshot from the shipped backend surface.
///
/// Interim defaults for backend concepts that do not exist yet (documented in
/// PROVIDER_HOME_RESOLVER_SPEC.md / resolved decisions):
/// - `bookingMode`: REQUEST until the provider-config field ships.
/// - `availability`: OPEN — vacation/closed are backend-managed and consumed
///   once the payload exists; never computed locally.
/// - `openCapacity`: 1 (never fully-booked) until available-slots is wired.
/// - `exceptions`/`alerts`/`nudges`: none until their sources exist.
/// - `thresholds`: [MaturityThresholds.fallback] until remote config ships.
class HomeRepositoryImpl implements HomeRepository {
  final HomeApiService _api;
  final AuthRepository _auth;
  final DateTime Function() _now;

  HomeRepositoryImpl(this._api, this._auth, {DateTime Function()? now})
      : _now = now ?? DateTime.now;

  @override
  Future<Either<Failure, HomeSnapshot>> fetchSnapshot() async {
    final sessionOr = await _auth.getCurrentSession();
    return sessionOr.fold(Left.new, (session) async {
      final providerId = session?.providerId;
      final status = session?.providerStatus;
      if (session == null || providerId == null || status == null) {
        return const Left(AuthFailure('نشست معتبر یافت نشد'));
      }

      final now = _now();
      final dayStart = DateTime(now.year, now.month, now.day);
      final dayEnd = dayStart.add(const Duration(days: 1));

      // Today + tomorrow in one call: today feeds the agenda, tomorrow feeds
      // the coming-up peek. This is the snapshot's one hard dependency.
      final List<Map<String, dynamic>> raw;
      try {
        raw = await _api.getProviderBookings(
          providerId,
          from: dayStart,
          to: dayStart.add(const Duration(days: 2)),
        );
      } on DioException {
        return const Left(ServerFailure('دریافت نوبت‌های امروز ناموفق بود'));
      }

      // Booking rows carry only ids for service/customer (verified live) —
      // resolve service names from the provider's own catalog (best-effort;
      // customer names need a backend cross-context enrichment, tracked).
      final serviceNames = await _fetchServiceNames(providerId);
      final bookings = raw.map((m) {
        final b = HomeApiService.toHomeBooking(m);
        if (b.serviceName.isNotEmpty) return b;
        final sid = HomeApiService.readString(m, const ['serviceId']);
        final name = serviceNames[sid];
        return name == null
            ? b
            : HomeBooking(
                id: b.id,
                start: b.start,
                clientName: b.clientName,
                clientPhone: b.clientPhone,
                serviceName: name,
                status: b.status,
              );
      }).toList();
      // A booking without a parsable start is assumed to be today's.
      final today = bookings
          .where((b) => b.start == null || b.start!.isBefore(dayEnd))
          .toList();
      final tomorrow = bookings.length - today.length;

      final signals = await _fetchSignals(providerId, today, now);
      return Right(_compose(status, today, tomorrow, signals, now));
    });
  }

  @override
  Future<Either<Failure, void>> confirmBooking(String id) =>
      _action(() => _api.confirmBooking(id), 'تأیید نوبت ناموفق بود');

  @override
  Future<Either<Failure, void>> declineBooking(String id,
          {required String reason}) =>
      _action(() => _api.cancelBooking(id, reason: reason),
          'رد نوبت ناموفق بود');

  @override
  Future<Either<Failure, void>> completeBooking(String id) =>
      _action(() => _api.completeBooking(id), 'ثبت تکمیل نوبت ناموفق بود');

  @override
  Future<Either<Failure, void>> markNoShow(String id) =>
      _action(() => _api.markNoShow(id), 'ثبت عدم حضور ناموفق بود');

  /// serviceId → name for the provider's catalog; empty on failure
  /// (best-effort enrichment only — rows fall back to time-only labels).
  Future<Map<String, String>> _fetchServiceNames(String providerId) async {
    try {
      final services = await _api.getProviderServices(providerId);
      return {
        for (final s in services)
          HomeApiService.readString(s, const ['id']):
              HomeApiService.readString(s, const ['name']),
      }..removeWhere((k, v) => k.isEmpty || v.isEmpty);
    } on DioException {
      return const {};
    }
  }

  /// Domain error codes worth a specific Persian message (verified live).
  static const Map<String, String> _errorCodeMessages = {
    'BOOKING_DEPOSIT_NOT_PAID':
        'پیش‌پرداخت این نوبت هنوز پرداخت نشده است و امکان تأیید وجود ندارد',
  };

  Future<Either<Failure, void>> _action(
    Future<void> Function() call,
    String failureMessage,
  ) async {
    try {
      await call();
      return const Right(null);
    } on DioException catch (e) {
      final code = HomeApiService.errorCode(e.response?.data);
      final mapped = code == null ? null : _errorCodeMessages[code];
      return Left(ServerFailure(mapped ?? failureMessage));
    }
  }

  /// Maturity signals from the statistics endpoint; on failure, synthesized
  /// from today's bookings so an established provider is never demoted to the
  /// Setup/Growth scaffold by a stats outage.
  Future<MaturitySignals> _fetchSignals(
    String providerId,
    List<HomeBooking> today,
    DateTime now,
  ) async {
    try {
      final allTime = await _api.getBookingStatistics(providerId);
      final trailing = await _api.getBookingStatistics(
        providerId,
        startDate: now.subtract(const Duration(days: 30)),
        endDate: now,
      );
      return MaturitySignals(
        // Completeness signals (staff/gallery/services) are not derivable from
        // the statistics endpoint; assume complete so a data gap can only
        // promote toward the agenda, never demote to the scaffold.
        profileComplete: true,
        totalBookingsAllTime: HomeApiService.readInt(
          allTime,
          const ['totalBookings', 'total', 'count', 'bookingsCount'],
        ),
        bookingsTrailing30d: HomeApiService.readInt(
          trailing,
          const ['totalBookings', 'total', 'count', 'bookingsCount'],
        ),
      );
    } on DioException {
      return MaturitySignals(
        profileComplete: true,
        totalBookingsAllTime: today.length,
        bookingsTrailing30d: today.length,
      );
    }
  }

  HomeSnapshot _compose(
    ProviderStatus status,
    List<HomeBooking> today,
    int tomorrowCount,
    MaturitySignals signals,
    DateTime now,
  ) {
    final active =
        today.where((b) => b.status != HomeBookingStatus.cancelled).toList()
          ..sort((a, b) {
            final sa = a.start, sb = b.start;
            if (sa == null || sb == null) return sa == sb ? 0 : (sa == null ? 1 : -1);
            return sa.compareTo(sb);
          });
    final pending =
        active.where((b) => b.status == HomeBookingStatus.pending).length;
    final allDone = active.isNotEmpty && active.every((b) => b.isDone);
    final hasUpcoming = active.any(
      (b) => !b.isDone && (b.start == null || !b.start!.isBefore(now)),
    );

    return HomeSnapshot(
      providerStatus: status,
      bookingMode: HomeBookingMode.request,
      availability: HomeAvailability.open,
      signals: signals,
      todayApptCount: active.length,
      allCompleted: allDone,
      hasUpcomingToday: hasUpcoming,
      pendingRequestCount: pending,
      todayBookings: active,
      tomorrowApptCount: tomorrowCount,
    );
  }
}
