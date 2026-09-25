import 'dart:convert';
import 'dart:typed_data';

import 'package:asan_rezerve_customer_app/features/bookings/data/datasources/bookings_remote_datasource.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// Rescheduling sends the new time exactly as the salon's wall-clock, like creating a booking does
/// (QA walkthrough 2026-09-22; FOLLOW-UPS #63). A UTC conversion moves the visit by the device's offset.
class _CapturingAdapter implements HttpClientAdapter {
  final requests = <RequestOptions>[];

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? requestStream,
      Future<void>? cancelFuture) async {
    requests.add(options);
    return ResponseBody.fromString(jsonEncode(const <String, dynamic>{}), 200, headers: {
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
}
