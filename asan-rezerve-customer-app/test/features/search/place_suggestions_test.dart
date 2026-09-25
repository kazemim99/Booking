import 'package:asan_rezerve_customer_app/core/location/geocoding_service.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// Typing a city, village or province suggests the places it could mean
/// (openspec/changes/customer-app-discovery-pass). The lookup goes through our
/// own API, never straight to Nominatim from a browser.
void main() {
  late Dio dio;
  late _RecordingAdapter adapter;

  setUp(() {
    dio = Dio(BaseOptions(baseUrl: 'https://example.test'));
    adapter = _RecordingAdapter();
    dio.httpClientAdapter = adapter;
  });

  test('what was typed comes back as places with coordinates', () async {
    adapter.body = '''
      [{"display_name":"پارس آباد، اردبیل","lat":"39.6461735","lon":"47.9185510"},
       {"display_name":"پارس آباد مغان","lat":"39.65","lon":"47.92"}]''';

    final places = await NominatimGeocodingService(dio).suggest('پارس آباد');

    expect(places, hasLength(2));
    expect(places.first.label, 'پارس آباد، اردبیل');
    expect(places.first.coordinates.latitude, closeTo(39.6461735, 0.0001));
    expect(adapter.lastPath, contains('/Geocoding/search'),
        reason: 'the browser must not call Nominatim directly');
  });

  test('one or two letters ask nothing at all', () async {
    final places = await NominatimGeocodingService(dio).suggest('پ');

    expect(places, isEmpty);
    expect(adapter.lastPath, isNull);
  });

  test('a failed lookup is simply no suggestions', () async {
    adapter.fail = true;

    final places = await NominatimGeocodingService(dio).suggest('پارس آباد');

    expect(places, isEmpty);
  });
}

class _RecordingAdapter implements HttpClientAdapter {
  String? lastPath;
  String body = '[]';
  bool fail = false;

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<List<int>>? stream,
      Future<void>? cancelFuture) async {
    lastPath = options.path;
    if (fail) throw DioException(requestOptions: options);
    return ResponseBody.fromString(body, 200,
        headers: {
          Headers.contentTypeHeader: [Headers.jsonContentType]
        });
  }

  @override
  void close({bool force = false}) {}
}
