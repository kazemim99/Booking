import 'dart:convert';
import 'dart:typed_data';

import 'package:asan_rezerve_customer_app/features/bookings/data/datasources/bookings_remote_datasource.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// Rescheduling sends the new time exactly as the salon's wall-clock, like creating a booking does
/// (QA walkthrough 2026-09-22; FOLLOW-UPS #63). A UTC conversion moves the visit by the device's offset.
class _CapturingAdapter implements HttpClientAdapter {
  final requests = <RequestOptions>[];
  Object answer = const <String, dynamic>{};

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? requestStream,
      Future<void>? cancelFuture) async {
    requests.add(options);
    return ResponseBody.fromString(jsonEncode(answer), 200, headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    });
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  test('a reschedule to 16:30 is sent as 16:30', () async {
    final adapter = _CapturingAdapter();
    final dio = Dio(BaseOptions(baseUrl: 'https://api.test'))..httpClientAdapter = adapter;

    await BookingsRemoteDataSource(serviceCatalogDio: dio).rescheduleBooking(
      bookingId: 'b1',
      newStartTime: DateTime.parse('2026-09-24T16:30:00'),
    );

    final data = adapter.requests.single.data as Map<String, dynamic>;
    expect(data['newStartTime'], '2026-09-24T16:30:00');
  });

  // reviews-and-reschedule-round2 item 9: the server closes the booking and opens a new one; the app follows the
  // new id. Today the endpoint names it only in its message.
  group('the new booking\'s id', () {
    const id = '3f2b8c1e-9a4d-4e6f-8b7a-1c2d3e4f5a6b';

    test('is read from today\'s message', () async {
      final adapter = _CapturingAdapter()
        ..answer = {'message': 'Booking rescheduled successfully. New booking ID: $id', 'timestamp': '2026-09-25'};
      final dio = Dio(BaseOptions(baseUrl: 'https://api.test'))..httpClientAdapter = adapter;

      final newId = await BookingsRemoteDataSource(serviceCatalogDio: dio).rescheduleBooking(
        bookingId: 'b1',
        newStartTime: DateTime.parse('2026-09-24T16:30:00'),
      );
      expect(newId, id);
    });

    test('prefers a newBookingId field, bare or in a data envelope', () {
      expect(BookingsRemoteDataSource.newBookingIdFrom({'newBookingId': 'n1', 'message': 'New booking ID: $id'}), 'n1');
      expect(BookingsRemoteDataSource.newBookingIdFrom({'data': {'newBookingId': 'n2'}}), 'n2');
      expect(BookingsRemoteDataSource.newBookingIdFrom({'data': {'message': 'New booking ID: $id'}}), id);
    });

    test('is null when the answer does not name it', () {
      expect(BookingsRemoteDataSource.newBookingIdFrom(const <String, dynamic>{}), isNull);
      expect(BookingsRemoteDataSource.newBookingIdFrom(''), isNull);
      expect(BookingsRemoteDataSource.newBookingIdFrom({'message': 'Booking $id rescheduled'}), isNull,
          reason: 'a GUID elsewhere in the message is not the new booking');
    });
  });
}
