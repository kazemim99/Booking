import 'dart:convert';
import 'dart:typed_data';

import 'package:asan_rezerve_customer_app/core/storage/secure_storage_service.dart';
import 'package:asan_rezerve_customer_app/features/bookings/data/datasources/bookings_remote_datasource.dart';
import 'package:asan_rezerve_customer_app/features/bookings/data/repositories/bookings_repository_impl.dart';
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';

/// A booking older than the newest fifty of either list is read on its own (UX review 2026-09-23, E.2).
///
/// The fixture is shaped like the real `GET /api/v1/Bookings/{id}` answer: `BookingsController.GetBookingById`
/// maps `BookingDetailsViewModel` to `BookingDetailsResponse` (camelCase), and `ApiResponseMiddleware` wraps it
/// in `{success, data}`. Its names differ from the list's `CustomerBookingDto`: `id` (not `bookingId`),
/// `providerBusinessName`, `staffProviderId` (a non-nullable Guid, all zeros when nobody was named) and the price
/// inside `paymentInfo`.
Map<String, dynamic> _details({
  String status = 'Completed',
  String startTime = '2026-05-10T14:00:00',
  String staffProviderId = '7b0f3c52-1d7e-4c89-9a51-2f5a3c0e9d11',
}) =>
    {
      'id': 'b-old',
      'customerId': 'user-1',
      'providerId': 'p1',
      'serviceId': 's1',
      'staffProviderId': staffProviderId,
      'serviceName': 'کوتاهی مو',
      'serviceCategory': '',
      'providerBusinessName': 'سالن نمونه',
      'providerCity': '',
      'startTime': startTime,
      'endTime': '2026-05-10T14:45:00',
      'durationMinutes': 45,
      'status': status,
      'paymentStatus': 'Pending',
      'paymentInfo': {
        'totalAmount': 250000,
        'currency': 'IRT',
        'depositAmount': 0,
        'paidAmount': 0,
        'refundedAmount': 0,
        'remainingAmount': 250000,
        'paymentStatus': 'Pending',
        'depositTransactionId': null,
        'fullPaymentTransactionId': null,
        'refundTransactionIds': <dynamic>[],
      },
      'customerNotes': null,
      'staffNotes': null,
      'policy': {
        'minAdvanceBookingHours': 0,
        'maxAdvanceBookingDays': 0,
        'cancellationWindowHours': 0,
        'cancellationFeePercentage': 0,
        'allowRescheduling': false,
        'rescheduleWindowHours': 0,
        'requireDeposit': false,
        'depositPercentage': 0,
      },
      'history': <dynamic>[],
      'createdAt': '2026-05-01T09:00:00',
      'lastModifiedAt': null,
      'confirmedAt': null,
      'completedAt': null,
      'cancelledAt': null,
    };

class _Adapter implements HttpClientAdapter {
  final int statusCode;
  final Object body;
  final requests = <RequestOptions>[];

  _Adapter(this.body, {this.statusCode = 200});

  @override
  Future<ResponseBody> fetch(
      RequestOptions options, Stream<Uint8List>? requestStream, Future<void>? cancelFuture) async {
    requests.add(options);
    return ResponseBody.fromString(jsonEncode(body), statusCode, headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    });
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  final now = DateTime(2026, 9, 23, 10);

  BookingsRepositoryImpl repositoryOver(_Adapter adapter) {
    final dio = Dio(BaseOptions(baseUrl: 'https://api.test'))..httpClientAdapter = adapter;
    return BookingsRepositoryImpl(
      remoteDataSource: BookingsRemoteDataSource(serviceCatalogDio: dio),
      storageService: SecureStorageService(const FlutterSecureStorage()),
      now: () => now,
    );
  }

  test('reads one booking by id and maps it like a list item', () async {
    final adapter = _Adapter({'success': true, 'data': _details()});

    final booking =
        (await repositoryOver(adapter).getBookingById('b-old')).getOrElse(() => throw StateError('expected a booking'));

    expect(adapter.requests.single.path, '/v1/Bookings/b-old');
    expect(booking.id, 'b-old');
    expect(booking.providerId, 'p1');
    expect(booking.providerName, 'سالن نمونه');
    expect(booking.serviceId, 's1');
    expect(booking.serviceName, 'کوتاهی مو');
    expect(booking.staffId, '7b0f3c52-1d7e-4c89-9a51-2f5a3c0e9d11');
    // Wall-clock: the digits the salon booked, not shifted by the device's zone.
    expect(booking.startTime, DateTime(2026, 5, 10, 14));
    expect(booking.durationMinutes, 45);
    expect(booking.price, 250000);
    expect(booking.currency, 'IRT');
    expect(booking.status, 'Completed');
    expect(booking.canReview, isTrue);
    expect(booking.canCancel, isFalse);
    expect(booking.canReschedule, isFalse);
  });

  test('an unwrapped body is read too', () async {
    final booking = (await repositoryOver(_Adapter(_details())).getBookingById('b-old'))
        .getOrElse(() => throw StateError('expected a booking'));

    expect(booking.id, 'b-old');
  });

  test('a future confirmed booking can be cancelled and rescheduled; nobody named means no staff', () async {
    final booking = (await repositoryOver(_Adapter({
      'data': _details(
        status: 'Confirmed',
        startTime: '2026-09-25T16:30:00',
        staffProviderId: '00000000-0000-0000-0000-000000000000',
      ),
    })).getBookingById('b-old'))
        .getOrElse(() => throw StateError('expected a booking'));

    expect(booking.canCancel, isTrue);
    expect(booking.canReschedule, isTrue);
    expect(booking.canReview, isFalse);
    // Rescheduling scopes slots to the staff id; an all-zero Guid would ask for nobody's times.
    expect(booking.staffId, isNull);
  });

  test('a booking the caller may not read is a failure, not a crash', () async {
    final result = await repositoryOver(_Adapter({'success': false}, statusCode: 403)).getBookingById('b-x');

    expect(result.isLeft(), isTrue);
  });
}
