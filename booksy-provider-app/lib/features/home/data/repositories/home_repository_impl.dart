import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';

import '../../../../core/errors/failures.dart';
import '../../../auth/domain/entities/provider_status.dart';
import '../../../auth/domain/repositories/auth_repository.dart';
import '../../../onboarding/domain/entities/onboarding_data.dart'
    show DayHours, GalleryImageUpload;
import '../../domain/entities/composer_models.dart';
import '../../domain/entities/home_booking.dart';
import '../../domain/entities/home_enums.dart';
import '../../domain/entities/home_inputs.dart';
import '../../domain/entities/home_snapshot.dart';
import '../../domain/entities/more_models.dart';
import '../../domain/entities/provider_client.dart';
import '../../domain/repositories/home_repository.dart';
import '../datasources/home_api_service.dart';

/// Composes the Home snapshot from the shipped backend surface.
///
/// Interim defaults for backend concepts that do not exist yet (documented in
/// PROVIDER_HOME_RESOLVER_SPEC.md / resolved decisions):
/// - `bookingMode`: REQUEST until the provider-config field ships.
/// - `availability`: consumed from the backend Holidays API (today matching a
///   holiday → closedToday; lookup failure degrades to OPEN). The VACATION
///   state stays dormant until a date-range concept exists.
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
      final List<HomeBooking> bookings;
      try {
        bookings = await _fetchEnrichedBookings(
          providerId,
          from: dayStart,
          to: dayStart.add(const Duration(days: 2)),
        );
      } on DioException {
        return const Left(ServerFailure('دریافت نوبت‌های امروز ناموفق بود'));
      }

      // A booking without a parsable start is assumed to be today's.
      final today = bookings
          .where((b) => b.start == null || b.start!.isBefore(dayEnd))
          .toList();
      final tomorrow = bookings.length - today.length;

      final signals = await _fetchSignals(providerId, today, now);
      final availability = await _todayAvailability(providerId, now);
      return Right(
          _compose(status, today, tomorrow, signals, now, availability));
    });
  }

  @override
  Future<Either<Failure, void>> confirmBooking(String id) =>
      _action(() => _api.confirmBooking(id), 'تأیید نوبت ناموفق بود');

  @override
  Future<Either<Failure, void>> declineBooking(String id,
      {required String reason}) async {
    final sessionOr = await _auth.getCurrentSession();
    return sessionOr.fold(Left.new, (session) {
      final userId = session?.user.id;
      if (userId == null) {
        return Future.value(
            const Left<Failure, void>(AuthFailure('نشست معتبر یافت نشد')));
      }
      return _action(
        () => _api.cancelBooking(id, reason: reason, cancelledBy: userId),
        'رد نوبت ناموفق بود',
      );
    });
  }

  @override
  Future<Either<Failure, void>> completeBooking(String id) =>
      _action(() => _api.completeBooking(id), 'ثبت تکمیل نوبت ناموفق بود');

  @override
  Future<Either<Failure, void>> markNoShow(String id) =>
      _action(() => _api.markNoShow(id), 'ثبت عدم حضور ناموفق بود');

  // ==================== calendar ====================

  @override
  Future<Either<Failure, List<HomeBooking>>> fetchBookings({
    required DateTime from,
    required DateTime to,
  }) {
    return _withProviderId((providerId) async {
      try {
        return Right(
            await _fetchEnrichedBookings(providerId, from: from, to: to));
      } on DioException {
        return const Left(ServerFailure('دریافت نوبت‌ها ناموفق بود'));
      }
    });
  }

  /// Raw range fetch + service-name enrichment (booking rows carry only ids
  /// for service/customer, verified live; customer names need a backend
  /// cross-context enrichment, tracked).
  Future<List<HomeBooking>> _fetchEnrichedBookings(
    String providerId, {
    required DateTime from,
    required DateTime to,
  }) async {
    final raw = await _api.getProviderBookings(providerId, from: from, to: to);
    final serviceNames = await _fetchServiceNames(providerId);
    return raw.map((m) {
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
  }

  // ==================== more hub ====================

  @override
  Future<Either<Failure, List<ComposerService>>> fetchServices() {
    return _withProviderId((providerId) async {
      try {
        final raw = await _api.getProviderServices(providerId);
        return Right(raw
            .map(_mapService)
            .where((s) => s.id.isNotEmpty)
            .toList());
      } on DioException {
        return const Left(ServerFailure('دریافت فهرست خدمات ناموفق بود'));
      }
    });
  }

  @override
  Future<Either<Failure, List<ProviderStaffMember>>> fetchStaff() {
    return _withProviderId((providerId) async {
      try {
        final raw = await _api.getProviderStaff(providerId);
        return Right(raw
            .map((s) => ProviderStaffMember(
                  id: HomeApiService.readString(s, const ['id']),
                  name: HomeApiService.readString(
                      s, const ['fullName', 'name', 'firstName']),
                  firstName:
                      HomeApiService.readString(s, const ['firstName']),
                  lastName: HomeApiService.readString(s, const ['lastName']),
                  phone: HomeApiService.readString(
                      s, const ['phoneNumber', 'phone']),
                  role: HomeApiService.readString(s, const ['role']),
                  isActive: s['isActive'] != false,
                ))
            .where((s) => s.id.isNotEmpty)
            .toList());
      } on DioException {
        return const Left(ServerFailure('دریافت فهرست تیم ناموفق بود'));
      }
    });
  }

  @override
  Future<Either<Failure, void>> addStaff({
    required String firstName,
    String? lastName,
    String? phoneNumber,
    String? role,
  }) {
    return _withProviderId((providerId) => _action(
          () => _api.addStaff(providerId,
              firstName: firstName,
              lastName: lastName,
              phoneNumber: phoneNumber,
              role: role),
          'افزودن عضو تیم ناموفق بود',
        ));
  }

  @override
  Future<Either<Failure, void>> updateStaff(
    String staffId, {
    required String firstName,
    String? lastName,
    String? phoneNumber,
    String? role,
  }) {
    return _withProviderId((providerId) => _action(
          () => _api.updateStaff(providerId, staffId,
              firstName: firstName,
              lastName: lastName,
              phoneNumber: phoneNumber,
              role: role),
          'ویرایش عضو تیم ناموفق بود',
        ));
  }

  @override
  Future<Either<Failure, void>> removeStaff(String staffId) {
    return _withProviderId((providerId) => _action(
          () => _api.removeStaff(providerId, staffId),
          'حذف عضو تیم ناموفق بود',
        ));
  }

  @override
  Future<Either<Failure, InsightsSummary>> fetchInsights() {
    return _withProviderId((providerId) async {
      try {
        final now = _now();
        final allTime = await _api.getBookingStatistics(providerId);
        final trailing = await _api.getBookingStatistics(
          providerId,
          startDate: now.subtract(const Duration(days: 30)),
          endDate: now,
        );
        double money(Map<String, dynamic> m, String key) =>
            switch (m[key]) { final num n => n.toDouble(), _ => 0.0 };
        return Right(InsightsSummary(
          totalBookings:
              HomeApiService.readInt(allTime, const ['totalBookings']),
          completedBookings:
              HomeApiService.readInt(allTime, const ['completedBookings']),
          cancelledBookings:
              HomeApiService.readInt(allTime, const ['cancelledBookings']),
          noShowBookings:
              HomeApiService.readInt(allTime, const ['noShowBookings']),
          totalRevenue: money(allTime, 'totalRevenue'),
          completedRevenue: money(allTime, 'completedRevenue'),
          currency: HomeApiService.readString(allTime, const ['currency']),
          bookingsTrailing30d:
              HomeApiService.readInt(trailing, const ['totalBookings']),
        ));
      } on DioException {
        return const Left(ServerFailure('دریافت گزارش‌ها ناموفق بود'));
      }
    });
  }

  @override
  Future<Either<Failure, BusinessProfile>> fetchBusinessProfile() {
    return _withProviderId((providerId) async {
      try {
        final details = await _api.getProviderDetails(providerId);
        return Right(BusinessProfile(
          businessName: HomeApiService.readString(
              details, const ['businessName', 'name']),
          description:
              HomeApiService.readString(details, const ['description']),
        ));
      } on DioException {
        return const Left(
            ServerFailure('دریافت مشخصات کسب‌وکار ناموفق بود'));
      }
    });
  }

  @override
  Future<Either<Failure, void>> updateBusinessProfile({
    required String businessName,
    String? description,
  }) {
    return _action(
      () => _api.updateBusinessInfo(
          businessName: businessName, description: description),
      'ذخیرهٔ مشخصات کسب‌وکار ناموفق بود',
    );
  }

  // ==================== gallery ====================

  @override
  Future<Either<Failure, List<GalleryImage>>> fetchGallery() {
    return _withProviderId((providerId) async {
      try {
        final raw = await _api.getGallery(providerId);
        final images = raw
            .map((g) => GalleryImage(
                  id: HomeApiService.readString(g, const ['id']),
                  thumbnailUrl: HomeApiService.readString(
                      g, const ['thumbnailUrl', 'mediumUrl', 'originalUrl']),
                  originalUrl: HomeApiService.readString(
                      g, const ['originalUrl', 'mediumUrl']),
                  isPrimary: g['isPrimary'] == true,
                  displayOrder:
                      HomeApiService.readInt(g, const ['displayOrder']),
                ))
            .where((g) => g.id.isNotEmpty)
            .toList()
          ..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));
        return Right(images);
      } on DioException {
        return const Left(ServerFailure('دریافت گالری ناموفق بود'));
      }
    });
  }

  @override
  Future<Either<Failure, void>> uploadGalleryImages(
      List<GalleryImageUpload> images) {
    return _withProviderId((providerId) => _action(
          () => _api.uploadGalleryImages(providerId, images),
          'بارگذاری تصاویر ناموفق بود',
        ));
  }

  @override
  Future<Either<Failure, void>> setPrimaryGalleryImage(String imageId) {
    return _withProviderId((providerId) => _action(
          () => _api.setPrimaryGalleryImage(providerId, imageId),
          'تغییر تصویر اصلی ناموفق بود',
        ));
  }

  @override
  Future<Either<Failure, void>> removeGalleryImage(String imageId) {
    return _withProviderId((providerId) => _action(
          () => _api.deleteGalleryImage(providerId, imageId),
          'حذف تصویر ناموفق بود',
        ));
  }

  // ==================== holidays ====================

  @override
  Future<Either<Failure, List<ProviderHoliday>>> fetchHolidays() {
    return _withProviderId((providerId) async {
      try {
        final raw = await _api.getHolidays(providerId);
        final holidays = raw
            .map(_mapHoliday)
            .whereType<ProviderHoliday>()
            .toList()
          ..sort((a, b) => a.date.compareTo(b.date));
        return Right(holidays);
      } on DioException {
        return const Left(ServerFailure('دریافت تعطیلات ناموفق بود'));
      }
    });
  }

  @override
  Future<Either<Failure, void>> addHoliday({
    required DateTime date,
    required String reason,
    bool isRecurring = false,
  }) {
    return _withProviderId((providerId) => _action(
          () => _api.addHoliday(providerId,
              date: date, reason: reason, isRecurring: isRecurring),
          'ثبت تعطیلی ناموفق بود',
        ));
  }

  @override
  Future<Either<Failure, void>> removeHoliday(String holidayId) {
    return _withProviderId((providerId) => _action(
          () => _api.deleteHoliday(providerId, holidayId),
          'حذف تعطیلی ناموفق بود',
        ));
  }

  static ProviderHoliday? _mapHoliday(Map<String, dynamic> h) {
    final date =
        DateTime.tryParse(HomeApiService.readString(h, const ['date']));
    final id = HomeApiService.readString(h, const ['id']);
    if (date == null || id.isEmpty) return null;
    return ProviderHoliday(
      id: id,
      date: DateTime(date.year, date.month, date.day),
      reason: HomeApiService.readString(h, const ['reason']),
      isRecurring: h['isRecurring'] == true,
    );
  }

  /// Best-effort: today's availability from holidays and closed-all-day
  /// exceptions. Failures degrade to OPEN — a side-signal never blocks the
  /// Home (spec: lookup failure degrades open).
  Future<HomeAvailability> _todayAvailability(
      String providerId, DateTime now) async {
    final today = DateTime(now.year, now.month, now.day);
    final results = await Future.wait([
      _holidayClosesToday(providerId, today),
      _exceptionClosesToday(providerId, today),
    ]);
    return results.contains(true)
        ? HomeAvailability.closedToday
        : HomeAvailability.open;
  }

  Future<bool> _holidayClosesToday(String providerId, DateTime today) async {
    try {
      final raw = await _api.getHolidays(providerId);
      return raw
          .map(_mapHoliday)
          .whereType<ProviderHoliday>()
          .any((h) => h.appliesTo(today));
    } on DioException {
      return false;
    }
  }

  Future<bool> _exceptionClosesToday(String providerId, DateTime today) async {
    try {
      final raw = await _api.getExceptions(providerId);
      return raw
          .map(_mapException)
          .whereType<AvailabilityException>()
          .any((e) => e.isClosed && e.appliesTo(today));
    } on DioException {
      return false;
    }
  }

  // ==================== block time (availability exceptions) ====================

  @override
  Future<Either<Failure, List<AvailabilityException>>> fetchExceptions() {
    return _withProviderId((providerId) async {
      try {
        final raw = await _api.getExceptions(providerId);
        final exceptions = raw
            .map(_mapException)
            .whereType<AvailabilityException>()
            .toList()
          ..sort((a, b) => a.date.compareTo(b.date));
        return Right(exceptions);
      } on DioException {
        return const Left(
            ServerFailure('دریافت ساعات استثنائی ناموفق بود'));
      }
    });
  }

  @override
  Future<Either<Failure, void>> addException({
    required DateTime date,
    String? openTime,
    String? closeTime,
    required String reason,
  }) {
    return _withProviderId((providerId) => _action(
          () => _api.addException(providerId,
              date: date,
              openTime: openTime,
              closeTime: closeTime,
              reason: reason),
          'مسدود کردن زمان ناموفق بود',
        ));
  }

  @override
  Future<Either<Failure, void>> removeException(String exceptionId) {
    return _withProviderId((providerId) => _action(
          () => _api.deleteException(providerId, exceptionId),
          'حذف ساعت استثنائی ناموفق بود',
        ));
  }

  static AvailabilityException? _mapException(Map<String, dynamic> e) {
    final date =
        DateTime.tryParse(HomeApiService.readString(e, const ['date']));
    final id = HomeApiService.readString(e, const ['id']);
    if (date == null || id.isEmpty) return null;
    final open = HomeApiService.readString(e, const ['openTime']);
    final close = HomeApiService.readString(e, const ['closeTime']);
    return AvailabilityException(
      id: id,
      date: DateTime(date.year, date.month, date.day),
      openTime: open.isEmpty ? null : open,
      closeTime: close.isEmpty ? null : close,
      reason: HomeApiService.readString(e, const ['reason']),
      isClosed: e['isClosed'] == true || (open.isEmpty && close.isEmpty),
    );
  }

  @override
  Future<Either<Failure, List<DayHours>>> fetchBusinessHours() {
    return _withProviderId((providerId) async {
      try {
        return Right(await _api.getBusinessHours(providerId));
      } on DioException {
        return const Left(ServerFailure('دریافت ساعات کاری ناموفق بود'));
      }
    });
  }

  @override
  Future<Either<Failure, void>> updateBusinessHours(List<DayHours> days) {
    return _withProviderId((providerId) => _action(
          () => _api.updateBusinessHours(providerId, days),
          'ذخیرهٔ ساعات کاری ناموفق بود',
        ));
  }

  // ---- Service CRUD (spec: provider-service-crud) ----

  @override
  Future<Either<Failure, void>> addService({
    required String name,
    required int durationMinutes,
    required double price,
    String? description,
  }) {
    return _withProviderId((providerId) => _action(
          () => _api.addService(providerId,
              name: name,
              durationMinutes: durationMinutes,
              price: price,
              description: description),
          'ثبت خدمت ناموفق بود',
        ));
  }

  @override
  Future<Either<Failure, void>> updateService(
    String serviceId, {
    required String name,
    required int durationMinutes,
    required double price,
    String? description,
  }) {
    return _withProviderId((providerId) => _action(
          () => _api.updateService(providerId, serviceId,
              name: name,
              durationMinutes: durationMinutes,
              price: price,
              description: description),
          'ویرایش خدمت ناموفق بود',
        ));
  }

  @override
  Future<Either<Failure, void>> removeService(String serviceId) {
    return _withProviderId((providerId) => _action(
          () => _api.deleteService(providerId, serviceId),
          'حذف خدمت ناموفق بود',
        ));
  }

  /// Shared raw→[ComposerService] mapping (composer catalog + services list).
  static ComposerService _mapService(Map<String, dynamic> s) => ComposerService(
        id: HomeApiService.readString(s, const ['id']),
        name: HomeApiService.readString(s, const ['name']),
        durationMinutes:
            HomeApiService.readInt(s, const ['duration', 'durationMinutes']),
        price: switch (s['basePrice'] ?? s['price']) {
          final num n => n.toDouble(),
          _ => 0.0,
        },
        description: HomeApiService.readString(s, const ['description']),
      );

  // ==================== clients ====================

  @override
  Future<Either<Failure, List<ProviderClient>>> fetchClients() {
    return _withProviderId((providerId) async {
      try {
        final raw = await _api.getProviderClients(providerId);
        return Right(raw
            .map((c) => ProviderClient(
                  customerId: HomeApiService.readString(
                      c, const ['customerId', 'id']),
                  name: HomeApiService.readString(c, const ['name']),
                  phone: HomeApiService.readString(c, const ['phone']),
                  totalBookings: HomeApiService.readInt(
                      c, const ['totalBookings', 'total']),
                  completedBookings: HomeApiService.readInt(
                      c, const ['completedBookings', 'completed']),
                  upcomingBookings: HomeApiService.readInt(
                      c, const ['upcomingBookings', 'upcoming']),
                  lastVisitAt: HomeApiService.bookingStart(
                      {'startTime': c['lastVisitAt']}),
                ))
            .where((c) => c.customerId.isNotEmpty)
            .toList());
      } on DioException {
        return const Left(ServerFailure('دریافت فهرست مشتریان ناموفق بود'));
      }
    });
  }

  // ==================== booking composer ====================

  @override
  Future<Either<Failure, ComposerCatalog>> fetchComposerCatalog() async {
    return _withProviderId((providerId) async {
      try {
        final results = await Future.wait([
          _api.getProviderServices(providerId),
          _api.getProviderStaff(providerId),
        ]);
        final services =
            results[0].map(_mapService).where((s) => s.id.isNotEmpty).toList();
        final staff = results[1]
            .map((s) => ComposerStaff(
                  id: HomeApiService.readString(s, const ['id']),
                  name: HomeApiService.readString(
                      s, const ['fullName', 'name', 'firstName']),
                ))
            .where((s) => s.id.isNotEmpty)
            .toList();
        return Right(ComposerCatalog(services: services, staff: staff));
      } on DioException {
        return const Left(ServerFailure('دریافت اطلاعات خدمات و تیم ناموفق بود'));
      }
    });
  }

  @override
  Future<Either<Failure, List<DateTime>>> fetchAvailableSlots({
    required String serviceId,
    required DateTime date,
    String? staffId,
  }) async {
    return _withProviderId((providerId) async {
      try {
        final slots = await _api.getAvailableSlots(
          providerId: providerId,
          serviceId: serviceId,
          date: date,
          staffId: staffId,
        );
        return Right(slots);
      } on DioException {
        return const Left(ServerFailure('دریافت زمان‌های خالی ناموفق بود'));
      }
    });
  }

  @override
  Future<Either<Failure, void>> createBooking({
    required String serviceId,
    required String staffId,
    required DateTime startTime,
    String? clientName,
    String? clientPhone,
    String? notes,
  }) async {
    return _withProviderId((providerId) async {
      try {
        await _api.createBooking(
          providerId: providerId,
          serviceId: serviceId,
          staffProviderId: staffId,
          startTime: startTime,
          customerNotes: walkInNotes(
            clientName: clientName,
            clientPhone: clientPhone,
            notes: notes,
          ),
        );
        return const Right(null);
      } on DioException catch (e) {
        final code = HomeApiService.errorCode(e.response?.data);
        final mapped = code == null ? null : _errorCodeMessages[code];
        return Left(ServerFailure(mapped ?? 'ثبت نوبت ناموفق بود'));
      }
    });
  }

  /// MVP walk-in identity convention (spec: provider-booking-composer):
  /// `مشتری حضوری: <name>[ — <phone>]` prepended to the free-form notes.
  static String walkInNotes({
    String? clientName,
    String? clientPhone,
    String? notes,
  }) {
    final name = clientName?.trim() ?? '';
    final phone = clientPhone?.trim() ?? '';
    final free = notes?.trim() ?? '';
    final walkIn = name.isEmpty && phone.isEmpty
        ? ''
        : 'مشتری حضوری: ${[name, phone].where((s) => s.isNotEmpty).join(' — ')}';
    return [walkIn, free].where((s) => s.isNotEmpty).join('\n');
  }

  /// Resolves the current providerId or fails with an auth failure.
  Future<Either<Failure, T>> _withProviderId<T>(
    Future<Either<Failure, T>> Function(String providerId) body,
  ) async {
    final sessionOr = await _auth.getCurrentSession();
    return sessionOr.fold(Left.new, (session) {
      final providerId = session?.providerId;
      if (providerId == null) {
        return Future.value(
            const Left<Failure, Never>(AuthFailure('نشست معتبر یافت نشد')));
      }
      return body(providerId);
    });
  }

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
    HomeAvailability availability,
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
      availability: availability,
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
