import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/features/booking/data/datasources/booking_remote_datasource.dart';

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
