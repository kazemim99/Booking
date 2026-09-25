import 'dart:convert';
import 'dart:typed_data';

import 'package:asan_rezerve_provider_app/features/home/data/datasources/home_api_service.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// Records the request body and answers 201.
class _Recorder implements HttpClientAdapter {
  Map<String, dynamic>? body;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    body = options.data as Map<String, dynamic>;
    return ResponseBody.fromString(
      jsonEncode({'data': {'id': 'b-1'}}),
      201,
      headers: {
        Headers.contentTypeHeader: ['application/json'],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

/// Booking times are salon wall-clock times: /Bookings/available-slots lists «…T09:00:00» (no zone)
/// and the server checks it against business hours as 09:00. The app converted it to UTC first, so
/// a 09:00 slot went out as 05:30Z and the server refused it as outside 09:00–18:00 (reproduced on
/// production, 2026-09-19). The time goes back exactly as listed.
void main() {
  late _Recorder recorder;
  late HomeApiService api;

  setUp(() {
    recorder = _Recorder();
    api = HomeApiService(
      Dio(BaseOptions(baseUrl: 'https://api.test'))..httpClientAdapter = recorder,
    );
  });

  Future<String> sentStartFor(DateTime slot) async {
    await api.createBooking(
      providerId: 'p-1',
      serviceId: 's-1',
      staffProviderId: 'm-1',
      startTime: slot,
    );
    return recorder.body!['startTime'] as String;
  }

  test('a listed slot goes back as the same wall-clock time', () async {
    final slot = DateTime.parse('2026-09-21T09:00:00'); // as the API lists it
    expect(await sentStartFor(slot), '2026-09-21T09:00:00');
  });

  test('never shifted by the device timezone, whatever kind of DateTime it is', () async {
    expect(await sentStartFor(DateTime.utc(2026, 9, 21, 9)), '2026-09-21T09:00:00');
    expect(await sentStartFor(DateTime(2026, 9, 21, 17, 30)), '2026-09-21T17:30:00');
  });
}
