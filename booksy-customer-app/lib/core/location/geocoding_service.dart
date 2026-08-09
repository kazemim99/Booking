import 'package:dio/dio.dart';

/// A resolved geographic coordinate.
class GeoCoordinates {
  final double latitude;
  final double longitude;
  const GeoCoordinates(this.latitude, this.longitude);
}

/// Resolves a place name (area / district / city) to coordinates.
/// Abstract so area/district search can be unit-tested with a fake.
abstract class GeocodingService {
  Future<GeoCoordinates?> geocode(String term);
}

/// Forward geocoding via OpenStreetMap's Nominatim — **keyless**, mirroring the
/// Provider app's `GeocodingService`. Persian output (`accept-language=fa`),
/// biased to Iran (`countrycodes=ir`). Best-effort: any error returns null.
///
/// Nominatim's usage policy requires an identifying User-Agent and a light
/// request rate — fine for occasional area/district lookups.
class NominatimGeocodingService implements GeocodingService {
  final Dio _dio;

  static const String _base = 'https://nominatim.openstreetmap.org';
  static const String _userAgent = 'BooksyCustomerApp/1.0 (discovery)';

  NominatimGeocodingService([Dio? dio])
      : _dio = dio ??
            Dio(BaseOptions(
              baseUrl: _base,
              responseType: ResponseType.json,
              headers: const {'User-Agent': _userAgent},
            ));

  @override
  Future<GeoCoordinates?> geocode(String term) async {
    if (term.trim().isEmpty) return null;
    try {
      final res = await _dio.get(
        '/search',
        queryParameters: {
          'q': term,
          'format': 'jsonv2',
          'accept-language': 'fa',
          'countrycodes': 'ir',
          'limit': 1,
        },
      );
      final data = res.data;
      if (data is List && data.isNotEmpty && data.first is Map) {
        final first = data.first as Map;
        final lat = double.tryParse('${first['lat']}');
        final lng = double.tryParse('${first['lon']}');
        if (lat != null && lng != null) return GeoCoordinates(lat, lng);
      }
      return null;
    } catch (_) {
      return null;
    }
  }
}
