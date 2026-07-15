import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_session.dart';
import 'package:booksy_provider_app/features/auth/domain/entities/provider_status.dart';
import 'package:booksy_provider_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:booksy_provider_app/features/home/data/datasources/home_api_service.dart';
import 'package:booksy_provider_app/features/home/data/repositories/home_repository_impl.dart';
import 'package:booksy_provider_app/features/home/domain/entities/home_enums.dart';
import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockApi extends Mock implements HomeApiService {}

class _MockAuth extends Mock implements AuthRepository {}

ProviderSession _session({String? providerId = 'p-1'}) => ProviderSession(
      accessToken: 'a',
      refreshToken: 'r',
      expiresIn: 900,
      user: const ProviderUser(
        id: 'u-1',
        phoneNumber: '09121234567',
        fullName: 'رضا محمدی',
      ),
      providerId: providerId,
      providerStatus: ProviderStatus.active,
      isNewProvider: false,
      requiresOnboarding: false,
    );

Map<String, dynamic> _booking(String status, {String? start}) => {
      'status': status,
      'startTime': ?start,
    };

void main() {
  late _MockApi api;
  late _MockAuth auth;

  // Fixed clock: 2026-07-15 12:00 local.
  final now = DateTime(2026, 7, 15, 12);

  setUp(() {
    api = _MockApi();
    auth = _MockAuth();
    when(() => auth.getCurrentSession())
        .thenAnswer((_) async => Right(_session()));
    // Statistics default: healthy operational numbers.
    when(() => api.getBookingStatistics(any(),
            startDate: any(named: 'startDate'), endDate: any(named: 'endDate')))
        .thenAnswer((_) async => {'totalBookings': 12});
    when(() => api.getBookingStatistics(any()))
        .thenAnswer((_) async => {'totalBookings': 50});
    when(() => api.getProviderServices(any())).thenAnswer((_) async => []);
  });

  HomeRepositoryImpl build() => HomeRepositoryImpl(api, auth, now: () => now);

  test('no usable session → AuthFailure', () async {
    when(() => auth.getCurrentSession())
        .thenAnswer((_) async => const Right(null));

    final result = await build().fetchSnapshot();

    expect(result.isLeft(), isTrue);
    result.fold((f) => expect(f, isA<AuthFailure>()), (_) {});
  });

  test('session without providerId → AuthFailure', () async {
    when(() => auth.getCurrentSession())
        .thenAnswer((_) async => Right(_session(providerId: null)));

    final result = await build().fetchSnapshot();

    expect(result.isLeft(), isTrue);
  });

  test('composes today counts from mixed booking statuses', () async {
    when(() => api.getProviderBookings(any(),
        from: any(named: 'from'), to: any(named: 'to'))).thenAnswer(
      (_) async => [
        _booking('Completed', start: '2026-07-15T09:00:00'),
        _booking('Pending', start: '2026-07-15T14:30:00'),
        _booking('Confirmed', start: '2026-07-15T16:00:00'),
        _booking('Cancelled', start: '2026-07-15T10:00:00'), // excluded
      ],
    );

    final result = await build().fetchSnapshot();

    final snap = result.getOrElse(() => throw StateError('expected Right'));
    expect(snap.todayApptCount, 3); // cancelled excluded
    expect(snap.pendingRequestCount, 1);
    expect(snap.allCompleted, isFalse);
    expect(snap.hasUpcomingToday, isTrue); // 14:30 ≥ 12:00
    expect(snap.providerStatus, ProviderStatus.active);
    expect(snap.signals.totalBookingsAllTime, 50);
    expect(snap.signals.bookingsTrailing30d, 12);
  });

  test('all done today → allCompleted, nothing upcoming', () async {
    when(() => api.getProviderBookings(any(),
        from: any(named: 'from'), to: any(named: 'to'))).thenAnswer(
      (_) async => [
        _booking('Completed', start: '2026-07-15T09:00:00'),
        _booking('NoShow', start: '2026-07-15T10:00:00'),
      ],
    );

    final snap = (await build().fetchSnapshot())
        .getOrElse(() => throw StateError('expected Right'));

    expect(snap.allCompleted, isTrue);
    expect(snap.hasUpcomingToday, isFalse);
  });

  test('bookings endpoint failure → ServerFailure (hard dependency)',
      () async {
    when(() => api.getProviderBookings(any(),
            from: any(named: 'from'), to: any(named: 'to')))
        .thenThrow(DioException(requestOptions: RequestOptions(path: '/x')));

    final result = await build().fetchSnapshot();

    result.fold((f) => expect(f, isA<ServerFailure>()),
        (_) => fail('expected Left'));
  });

  test('statistics failure degrades to synthesized signals — an established '
      'provider is never demoted to the scaffold', () async {
    when(() => api.getProviderBookings(any(),
        from: any(named: 'from'), to: any(named: 'to'))).thenAnswer(
      (_) async => [
        _booking('Confirmed', start: '2026-07-15T16:00:00'),
        _booking('Confirmed', start: '2026-07-15T17:00:00'),
      ],
    );
    when(() => api.getBookingStatistics(any(),
            startDate: any(named: 'startDate'), endDate: any(named: 'endDate')))
        .thenThrow(DioException(requestOptions: RequestOptions(path: '/x')));
    when(() => api.getBookingStatistics(any()))
        .thenThrow(DioException(requestOptions: RequestOptions(path: '/x')));

    final snap = (await build().fetchSnapshot())
        .getOrElse(() => throw StateError('expected Right'));

    // Synthesized from today's bookings → TRACTION-ish, never SETUP.
    expect(snap.signals.totalBookingsAllTime, 2);
    expect(snap.signals.profileComplete, isTrue);
  });

  group('HomeApiService pure parsers', () {
    test('unwrapList handles bare arrays and paged envelopes', () {
      final bare = [
        {'a': 1},
      ];
      expect(HomeApiService.unwrapList(bare), hasLength(1));
      expect(
        HomeApiService.unwrapList({
          'items': [
            {'a': 1},
            {'b': 2},
          ],
        }),
        hasLength(2),
      );
      expect(
        HomeApiService.unwrapList({
          'data': [
            {'a': 1},
          ],
        }),
        hasLength(1),
      );
      expect(HomeApiService.unwrapList('garbage'), isEmpty);
      expect(HomeApiService.unwrapList(null), isEmpty);
    });

    test('bookingStatus canonicalizes case and tolerates absence', () {
      expect(HomeApiService.bookingStatus({'status': 'Pending'}), 'pending');
      expect(
        HomeApiService.bookingStatus({'bookingStatus': 'NoShow'}),
        'noshow',
      );
      expect(HomeApiService.bookingStatus({}), '');
    });

    test('bookingStart reads the first recognized key', () {
      expect(
        HomeApiService.bookingStart({'startTime': '2026-07-15T09:00:00'}),
        DateTime(2026, 7, 15, 9),
      );
      expect(
        HomeApiService.bookingStart({'bookingDate': '2026-07-15T10:00:00'}),
        DateTime(2026, 7, 15, 10),
      );
      expect(HomeApiService.bookingStart({'startTime': 'not-a-date'}), isNull);
      expect(HomeApiService.bookingStart({}), isNull);
    });

    test('readInt accepts int, num, and numeric strings', () {
      expect(HomeApiService.readInt({'total': 5}, const ['total']), 5);
      expect(HomeApiService.readInt({'total': 5.0}, const ['total']), 5);
      expect(HomeApiService.readInt({'total': '5'}, const ['total']), 5);
      expect(
        HomeApiService.readInt({}, const ['total'], fallback: 7),
        7,
      );
    });
  });

  group('live-verified shapes (2026-07-15 backend run)', () {
    test('"Requested" status counts as a pending request', () async {
      when(() => api.getProviderBookings(any(),
          from: any(named: 'from'), to: any(named: 'to'))).thenAnswer(
        (_) async => [
          _booking('Requested', start: '2026-07-15T20:30:00+03:30'),
        ],
      );

      final snap = (await build().fetchSnapshot())
          .getOrElse(() => throw StateError('expected Right'));

      expect(snap.pendingRequestCount, 1);
      expect(snap.todayBookings.single.start!.isUtc, isFalse); // toLocal
    });

    test('service names are resolved from the provider catalog', () async {
      when(() => api.getProviderBookings(any(),
          from: any(named: 'from'), to: any(named: 'to'))).thenAnswer(
        (_) async => [
          {
            'id': 'b1',
            'serviceId': 'svc-1',
            'status': 'Requested',
            'startTime': '2026-07-15T20:30:00+03:30',
          },
        ],
      );
      when(() => api.getProviderServices(any())).thenAnswer(
        (_) async => [
          {'id': 'svc-1', 'name': 'Haircut'},
        ],
      );

      final snap = (await build().fetchSnapshot())
          .getOrElse(() => throw StateError('expected Right'));

      expect(snap.todayBookings.single.serviceName, 'Haircut');
    });

    test('deposit-gated confirm surfaces the mapped Persian message',
        () async {
      when(() => api.confirmBooking(any())).thenThrow(
        DioException(
          requestOptions: RequestOptions(path: '/x'),
          response: Response(
            requestOptions: RequestOptions(path: '/x'),
            statusCode: 400,
            data: {
              'success': false,
              'error': {'code': 'BOOKING_DEPOSIT_NOT_PAID'},
            },
          ),
        ),
      );

      final result = await build().confirmBooking('b1');

      result.fold(
        (f) => expect(f.message, contains('پیش‌پرداخت')),
        (_) => fail('expected Left'),
      );
    });

    test('unwrapList handles the nested {data:{items:[...]}} envelope', () {
      expect(
        HomeApiService.unwrapList({
          'success': true,
          'data': {
            'items': [
              {'id': 's1'},
            ],
            'totalCount': 1,
          },
        }),
        hasLength(1),
      );
    });

    test('unwrapMap peels the {success, data:{...}} envelope', () {
      expect(
        HomeApiService.unwrapMap({
          'success': true,
          'data': {'totalBookings': 2},
        }),
        {'totalBookings': 2},
      );
      expect(HomeApiService.unwrapMap({'totalBookings': 2}),
          {'totalBookings': 2});
      expect(HomeApiService.unwrapMap(null), isEmpty);
    });

    test('errorCode extracts the wrapped domain error code', () {
      expect(
        HomeApiService.errorCode({
          'error': {'code': 'BOOKING_DEPOSIT_NOT_PAID'},
        }),
        'BOOKING_DEPOSIT_NOT_PAID',
      );
      expect(HomeApiService.errorCode({'message': 'x'}), isNull);
      expect(HomeApiService.errorCode('garbage'), isNull);
    });
  });

  test('degraded defaults hold until backend concepts ship', () async {
    when(() => api.getProviderBookings(any(),
            from: any(named: 'from'), to: any(named: 'to')))
        .thenAnswer((_) async => []);

    final snap = (await build().fetchSnapshot())
        .getOrElse(() => throw StateError('expected Right'));

    expect(snap.bookingMode, HomeBookingMode.request);
    expect(snap.availability, HomeAvailability.open);
    expect(snap.openCapacity, greaterThan(0)); // never fully-booked yet
    expect(snap.exceptionCount, 0);
    expect(snap.alertCount, 0);
  });
}
