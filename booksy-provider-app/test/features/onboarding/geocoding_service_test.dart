import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:booksy_provider_app/features/onboarding/data/datasources/geocoding_service.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('GeocodingService.shortenAddress', () {
    test('drops the administrative tail, postcode and country', () {
      const displayName =
          'خانه سینما ساختمان شماره ۲, 28, وصال شیرازی, دانشگاه تهران, ناحیه ۲, '
          'منطقه ۶ شهر تهران, شهرداری منطقه شش ناحیه سه, تهران, بخش مرکزی تهران, '
          'شهرستان تهران, استان تهران, 14168-54528, ایران';

      final short = GeocodingService.shortenAddress(
        displayName,
        city: 'تهران',
        state: 'استان تهران',
      );

      expect(
        short,
        'خانه سینما ساختمان شماره ۲، 28، وصال شیرازی، دانشگاه تهران، شهرداری منطقه شش ناحیه سه',
      );
      // The noisy administrative parts are gone.
      expect(short.contains('استان'), isFalse);
      expect(short.contains('شهرستان'), isFalse);
      expect(short.contains('ایران'), isFalse);
      expect(short.contains('14168'), isFalse);
    });

    test('keeps a short address unchanged (minus the country)', () {
      final short = GeocodingService.shortenAddress(
        'پلاک ۱۲, خیابان ولیعصر, ایران',
        city: '',
        state: '',
      );
      expect(short, 'پلاک ۱۲، خیابان ولیعصر');
    });

    test('caps the number of kept parts', () {
      final short = GeocodingService.shortenAddress(
        'الف, ب, پ, ت, ث, ج, چ, ح',
        maxParts: 3,
      );
      expect(short, 'الف، ب، پ');
    });
  });

  group(
    'GeocodingService.formatAddress (Iranian order: area -> street -> number)',
    () {
      test("the user's reported pin: neighbourhood first, then the alley", () {
        // Nominatim's structured `address` for a pin in Parsabad (2026-09-19).
        const addr = {
          'road': 'کوچه ۵ سهند',
          'neighbourhood': 'محله طالقانی',
          'town': 'شهر پارس آباد',
          'district': 'بخش مرکزی',
          'county': 'شهرستان پارس آباد',
          'state': 'استان اردبیل',
          'postcode': '56919-44471',
          'country': 'ایران',
        };

        expect(
          GeocodingService.formatAddress(addr),
          'محله طالقانی، کوچه ۵ سهند',
        );
      });

      test('adds the plate number and the place name after the street', () {
        const addr = {
          'amenity': 'خانه سینما',
          'house_number': '28',
          'road': 'خیابان وصال شیرازی',
          'suburb': 'دانشگاه تهران',
          'city': 'تهران',
        };

        expect(
          GeocodingService.formatAddress(addr),
          'دانشگاه تهران، خیابان وصال شیرازی، پلاک 28، خانه سینما',
        );
      });

      test('a street alone is still an address', () {
        expect(
          GeocodingService.formatAddress(const {'road': 'خیابان ولیعصر'}),
          'خیابان ولیعصر',
        );
      });

      test('never repeats the city, which the city field already holds', () {
        const addr = {
          'neighbourhood': 'شهر پارس آباد',
          'road': 'کوچه ۲',
          'town': 'شهر پارس آباد',
        };
        expect(GeocodingService.formatAddress(addr), 'کوچه ۲');
      });

      test(
        'without structured parts, falls back to display_name, general to specific',
        () {
          final formatted = GeocodingService.formatAddress(
            const {},
            displayName: 'پلاک ۱۲, خیابان ولیعصر, ایران',
          );
          expect(formatted, 'خیابان ولیعصر، پلاک ۱۲');
        },
      );
    },
  );

  group('goes through our API, never the geocoder directly', () {
    // The browser call to nominatim.openstreetmap.org failed on the user's network
    // (2026-09-19) while the server reached it fine, and it made every visitor an
    // unidentified client of a shared free service. The server does the lookup now.
    late List<RequestOptions> sent;
    late Dio dio;

    setUp(() {
      sent = [];
      dio = Dio(BaseOptions(baseUrl: 'https://back.example.ir/api'))
        ..httpClientAdapter = _RecordingAdapter(sent);
    });

    test('forward lookup asks our geocoding endpoint', () async {
      await GeocodingService(dio).geocode('پارس آباد, اردبیل');

      final req = sent.single;
      expect(req.path, '/v1/Geocoding/search');
      expect(req.uri.host, 'back.example.ir');
      expect(req.queryParameters['q'], 'پارس آباد, اردبیل');
    });

    test('reverse lookup asks our geocoding endpoint', () async {
      await GeocodingService(dio).reverseGeocode(39.643, 47.897);

      final req = sent.single;
      expect(req.path, '/v1/Geocoding/reverse');
      expect(req.uri.host, 'back.example.ir');
      expect(req.queryParameters['lat'], 39.643);
      expect(req.queryParameters['lon'], 47.897);
    });

    test(
      'an unavailable lookup yields null, so the form keeps what was typed',
      () async {
        final failing = Dio(BaseOptions(baseUrl: 'https://back.example.ir/api'))
          ..httpClientAdapter = _RecordingAdapter([], status: 503);

        expect(await GeocodingService(failing).geocode('تهران'), isNull);
        expect(await GeocodingService(failing).reverseGeocode(1, 2), isNull);
      },
    );
  });
}

/// Records outgoing requests and answers with an empty result.
class _RecordingAdapter implements HttpClientAdapter {
  final List<RequestOptions> sent;
  final int status;
  _RecordingAdapter(this.sent, {this.status = 200});

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    sent.add(options);
    return ResponseBody.fromString(
      options.path.endsWith('search') ? '[]' : '{}',
      status,
      headers: {
        Headers.contentTypeHeader: ['application/json'],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}
