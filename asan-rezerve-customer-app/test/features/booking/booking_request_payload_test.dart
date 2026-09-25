import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:asan_rezerve_customer_app/features/booking/data/datasources/booking_remote_datasource.dart';

/// Wire-format tests for the multi-service booking request.
///
/// These sit at the HTTP boundary on purpose: the whole point of multi-service
/// booking is that *every* selected service reaches the backend, and that the
/// availability query asks for a slot long enough for the combined duration.
/// A repository-level fake could not prove either.

/// Captures the request Dio was about to send and answers with a canned body.
class _CapturingAdapter implements HttpClientAdapter {
  final List<RequestOptions> requests = [];
  final Map<String, dynamic> body;
  final int statusCode;

  _CapturingAdapter({
    this.body = const <String, dynamic>{},
    this.statusCode = 200,
  });

  RequestOptions get lastRequest => requests.last;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(options);
    return ResponseBody.fromString(
      jsonEncode(body),
      statusCode,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

({BookingRemoteDataSource source, _CapturingAdapter adapter}) _build({
  Map<String, dynamic> body = const <String, dynamic>{},
  int statusCode = 200,
}) {
  final adapter = _CapturingAdapter(body: body, statusCode: statusCode);
  final dio = Dio(BaseOptions(baseUrl: 'https://api.test'))
    ..httpClientAdapter = adapter;
  return (
    source: BookingRemoteDataSource(serviceCatalogDio: dio),
    adapter: adapter,
  );
}

void main() {
  group('createBooking payload', () {
    test('sends every selected service, with the first as the required id',
        () async {
      final harness = _build(body: {'id': 'b1'}, statusCode: 201);

      await harness.source.createBooking(
        providerId: 'p1',
        serviceId: 's1',
        staffProviderId: 'st1',
        startTime: DateTime.utc(2026, 8, 1, 10),
        serviceIds: const ['s1', 's2', 's3'],
      );

      final data = harness.adapter.lastRequest.data as Map<String, dynamic>;
      expect(data['serviceIds'], ['s1', 's2', 's3']);
      expect(data['serviceId'], 's1',
          reason: 'the backend still marks the single id required');
      expect(data['providerId'], 'p1');
      expect(data['staffProviderId'], 'st1');
    });

    test('omits serviceIds when the caller passes none', () async {
      final harness = _build(body: {'id': 'b1'}, statusCode: 201);

      await harness.source.createBooking(
        providerId: 'p1',
        serviceId: 's1',
        staffProviderId: 'st1',
        startTime: DateTime.utc(2026, 8, 1, 10),
      );

      final data = harness.adapter.lastRequest.data as Map<String, dynamic>;
      expect(data.containsKey('serviceIds'), isFalse);
      expect(data['serviceId'], 's1');
    });
  });

  // QA walkthrough 2026-09-22: the server lists and stores booking times as the SALON'S WALL-CLOCK, with no zone
  // (FOLLOW-UPS #63). The app used to convert the chosen slot to UTC, so in Tehran a "14:00" slot went out as
  // 10:30Z and was booked — and conflict-checked — at 10:30, leaving the real 14:00 free for someone else. The
  // provider app had the same defect and was fixed on 2026-09-19 (0e79e227).
  group('booking times travel as the salon wall-clock', () {
    test('a 14:00 slot is sent as 14:00, never shifted by the device zone', () async {
      final harness = _build(body: {'id': 'b1'}, statusCode: 201);

      // Exactly how a slot arrives: parsed from the server's zone-less "2026-09-23T14:00:00", i.e. LOCAL time.
      await harness.source.createBooking(
        providerId: 'p1',
        serviceId: 's1',
        staffProviderId: 'st1',
        startTime: DateTime.parse('2026-09-23T14:00:00'),
      );

      final data = harness.adapter.lastRequest.data as Map<String, dynamic>;
      expect(data['startTime'], '2026-09-23T14:00:00');
    });
  });

  // QA walkthrough 2026-09-22: a normal open day said only "no free time for this day". The server explains why
  // (closed that weekday, the day is shorter than the visit, nobody qualified) and the app threw it away.
  group('an empty day carries the reason the salon gave', () {
    test('the explanation from the server is kept, not just the empty list', () async {
      final harness = _build(body: {
        'data': {
          'slots': <dynamic>[],
          'validationMessages': ['مجموعه در این روز تعطیل است.'],
        },
      });

      final day = await harness.source.getAvailableSlots(
        providerId: 'p1',
        serviceId: 's1',
        date: DateTime(2026, 9, 23),
      );

      expect((day['slots'] as List).isEmpty, isTrue);
      expect((day['validationMessages'] as List).first, 'مجموعه در این روز تعطیل است.');
    });
  });

  group('getAvailableSlots query', () {
    test('sends every selected service as repeated ServiceIds parameters',
        () async {
      final harness = _build(body: {'slots': <dynamic>[]});

      await harness.source.getAvailableSlots(
        providerId: 'p1',
        serviceId: 's1',
        date: DateTime.utc(2026, 8, 1),
        serviceIds: const ['s1', 's2'],
      );

      // Repeated keys are how ASP.NET binds a list from the query string, so
      // assert the built URI rather than the map.
      final uri = harness.adapter.lastRequest.uri;
      expect(uri.queryParametersAll['ServiceIds'], ['s1', 's2']);
      expect(uri.queryParameters['ServiceId'], 's1');
      expect(uri.queryParameters['ProviderId'], 'p1');
    });

    test('omits ServiceIds when the caller passes none', () async {
      final harness = _build(body: {'slots': <dynamic>[]});

      await harness.source.getAvailableSlots(
        providerId: 'p1',
        serviceId: 's1',
        date: DateTime.utc(2026, 8, 1),
      );

      final uri = harness.adapter.lastRequest.uri;
      expect(uri.queryParametersAll.containsKey('ServiceIds'), isFalse);
      expect(uri.queryParameters['ServiceId'], 's1');
    });
  });
}
